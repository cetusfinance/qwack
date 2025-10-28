using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Math;
using Qwack.Models.MCModels;
using Qwack.Models.Models;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Qwack.Utils.Parallel;
using static System.Math;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk
{
    public static class BasicMetrics
    {

        public static Dictionary<string, ICube> ComputeBumpedScenarios(Dictionary<string, IPvModel> models, Currency ccy)
        {
            var results = new Tuple<string, ICube>[models.Count];
            var bModelList = models.ToList();
            ParallelUtils.Instance.For(0, results.Length, 1, ii =>
            {
                var bModel = bModelList[ii];
                var bumpedPVCube = bModel.Value.PV(ccy);
                results[ii] = new Tuple<string, ICube>(bModel.Key, bumpedPVCube);
            }).Wait();
            return results.ToDictionary(k => k.Item1, v => v.Item2);
        }

        public static double GetStrike(this IInstrument ins) => ins switch
        {
            null => 0.0,
            EuropeanOption euo => euo.Strike,
            FuturesOption fuo => fuo.Strike,
            _ => 0.0,
        };

        public static ICube AssetVega(this IPvModel pvModel, Currency reportingCcy, bool parallelize = true)
        {
            var bumpSize = -0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "Strike", typeof(double) },
                { "RefPrice", typeof(double) },
                { Consts.Cubes.Portfolio, typeof(string)  }

            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var volObj = model.GetVolSurface(surfaceName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments.Where(x => (x is IHasVega || (x is CashWrapper cw && cw.UnderlyingInstrument is IHasVega)) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var strikesByTradeId = subPortfolio.Instruments.ToDictionary(t => t.TradeId, t => t.GetStrike());

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedSurfaces.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var trdId = bumpedRows[i].MetaData[tidIx] as string;
                            var pillarDate = bCurve.Value.PillarDatesForLabel(bCurve.Key);
                            var fwdCurve = model.VanillaModel.GetPriceCurve(bCurve.Value.AssetId);
                            var fwdPrice = fwdCurve?.GetPriceForDate(pillarDate) ?? 100;
                            var cleanVol = model.VanillaModel.GetVolForDeltaStrikeAndDate(bCurve.Value.AssetId, pillarDate, 0.5);
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, trdId },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", pillarDate },
                                { PointLabel, bCurve.Key },
                                { Metric, "Vega" },
                                { "Strike", strikesByTradeId[trdId] },
                                { "Portfolio", bumpedRows[i].MetaData[pfIx]  },
                                { "RefPrice", cleanVol },
                            };

                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                }, !(parallelize)).Wait();
            }

            return cube.Sort(new List<string> { AssetId, "PointDate", TradeType });
        }

        public static ICube AssetVegaWavey(this IPvModel pvModel, Currency reportingCcy)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "Strike", typeof(double) },
                { "RefPrice", typeof(double) },
                { Consts.Cubes.Portfolio, typeof(string)  }

            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var volObj = model.GetVolSurface(surfaceName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments.Where(x => (x is IHasVega || (x is CashWrapper cw && cw.UnderlyingInstrument is IHasVega)) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var strikesByTradeId = subPortfolio.Instruments.ToDictionary(t => t.TradeId, t => t.GetStrike());

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var bumpedSurfaces = volObj.GetATMVegaWaveyScenarios(bumpSize, lastDateInBook);
                var maturityDict = bumpedSurfaces.Keys.ToDictionary(x => x, volObj.PillarDatesForLabel);

                foreach(var bCurve in bumpedSurfaces.OrderByDescending(x=> maturityDict[x.Key]))
                {
                    var pvRows = pvCube.GetAllRows();
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var trdId = bumpedRows[i].MetaData[tidIx] as string;
                            var pillarDate = bCurve.Value.PillarDatesForLabel(bCurve.Key);
                            var fwdCurve = model.VanillaModel.GetPriceCurve(bCurve.Value.AssetId);
                            var fwdPrice = fwdCurve?.GetPriceForDate(pillarDate) ?? 100;
                            var cleanVol = model.VanillaModel.GetVolForDeltaStrikeAndDate(bCurve.Value.AssetId, pillarDate, 0.5);
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, trdId },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", pillarDate },
                                { PointLabel, bCurve.Key },
                                { Metric, "Vega" },
                                { "Strike", strikesByTradeId[trdId] },
                                { "Portfolio", bumpedRows[i].MetaData[pfIx]  },
                                { "RefPrice", cleanVol },
                            };

                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }

                    pvCube = bumpedPVCube;
                }
            }

            return cube.Sort(new List<string> { AssetId, "PointDate", TradeType });
        }


        public static ICube AssetSegaRega(this IPvModel pvModel, Currency reportingCcy)
        {
            var bumpSize = 0.001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                if (model.GetVolSurface(surfaceName) is not RiskyFlySurface volObj)
                    continue;

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments.Where(x => (x is IHasVega) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);
     
                var bumpedSurfacesSega = volObj.GetSegaScenarios(bumpSize, lastDateInBook);
                var bumpedSurfacesRega = volObj.GetRegaScenarios(bumpSize, lastDateInBook);

                var t1 = ParallelUtils.Instance.Foreach(bumpedSurfacesSega.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //sega and rega quoted as for a 10bp move
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.001;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Sega" },
                                { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx]  }

                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                });

                var t2 = ParallelUtils.Instance.Foreach(bumpedSurfacesRega.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //sega and rega quoted as for a 10bp move
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.001;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Rega" },
                                { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx]  }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                });

                var tasks = new[] { t1, t2 };
                Task.WaitAll(tasks);
            }

            return cube.Sort();
        }

        public static ICube FxVega(this IPvModel pvModel, Currency reportingCcy)
        {
            var bumpSize = 0.001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) },
                { "RefPrice", typeof(double) },
            };

            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            var subPortfolio = new Portfolio()
            {
                Instruments = pvModel.Portfolio.Instruments.Where(x => x is IHasVega || (x is CashWrapper cw && cw.UnderlyingInstrument is IHasVega)).ToList()
            };

            if (subPortfolio.Instruments.Count == 0)
                return cube;

            var lastDateInBook = subPortfolio.LastSensitivityDate;
            using var basePvModel = pvModel.Rebuild(model, subPortfolio);
            var pvCube = basePvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);
            var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

            foreach (var surface in model.FundingModel.VolSurfaces)
            {
                var volObj = surface.Value;
                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedSurfaces.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.FundingModel.VolSurfaces[surface.Key] = bCurve.Value;
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var pillarDate = bCurve.Value.PillarDatesForLabel(bCurve.Key);
                            var cleanVol = model.VanillaModel.GetFxVolForDeltaStrikeAndDate(surface.Key, pillarDate, 0.5);

                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surface.Key },
                                { "PointDate", pillarDate},
                                { PointLabel, bCurve.Key },
                                { Metric, "Vega" },
                                { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx]  },
                                { "RefPrice", cleanVol },

                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                }).Wait();
            }

            return cube.Sort();
        }

        public static ICube FxSegaRega(this IPvModel pvModel, Currency reportingCcy)
        {
            var bumpSize = 0.001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            foreach (var surfaceName in model.FundingModel.VolSurfaces.Keys)
            {
                if (model.GetVolSurface(surfaceName) is not RiskyFlySurface volObj)
                    continue;

                var ccys = new [] { surfaceName.Substring(0,3), surfaceName.Substring(surfaceName.Length-3,3) };

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments.Where(x => (x is IHasVega) && ccys.Contains(x.Currency.Ccy)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                var bumpedSurfacesSega = volObj.GetSegaScenarios(bumpSize, lastDateInBook);
                var bumpedSurfacesRega = volObj.GetRegaScenarios(bumpSize, lastDateInBook);

                var t1 = ParallelUtils.Instance.Foreach(bumpedSurfacesSega.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.FundingModel.VolSurfaces[surfaceName] = bCurve.Value;
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //sega and rega quoted as for a 10bp move
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.001;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Sega" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                });

                var t2 = ParallelUtils.Instance.Foreach(bumpedSurfacesRega.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.FundingModel.VolSurfaces[surfaceName] = bCurve.Value;
                    using var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //sega and rega quoted as for a 10bp move
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.001;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Rega" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, vega);
                        }
                    }
                });

                var tasks = new[] { t1, t2 };
                Task.WaitAll(tasks);
            }

            return cube.Sort();
        }


        public static ICube AssetDeltaSingleCurve(this IPvModel pvModel, string assetId, bool computeGamma = false, bool isSparseLMEMode = false, ICalendarProvider calendars = null, bool parallelize = false)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "CurveType", typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;
            model.BuildDependencyTree();

            var curveName = (model.Curves.Where(x => x.AssetId == assetId).FirstOrDefault()?.Name) ?? throw new Exception($"Unable to find curve with asset id {assetId}");

            var curveObj = model.GetPriceCurve(curveName);
            var linkedCurves = model.GetDependentCurves(curveName);
            var allLinkedCurves = model.GetAllDependentCurves(curveName);

            var subPortfolio = new Portfolio()
            {
                Instruments = pvModel.Portfolio.Instruments
                .Where(x => (x is IAssetInstrument ia) &&
                (ia.AssetIds.Contains(curveObj.AssetId) || ia.AssetIds.Any(aid => allLinkedCurves.Contains(aid))))
                .ToList()
            };

            if (subPortfolio.Instruments.Count == 0)
                return cube;

            var lastDateInBook = subPortfolio.LastSensitivityDate;
            model.AttachPortfolio(subPortfolio);
            var pvCube = pvModel.PV(curveObj.Currency);
            var pvRows = pvCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            Dictionary<string, IPriceCurve> bumpedCurves;
            Dictionary<string, IPriceCurve> bumpedDownCurves;

            if (isSparseLMEMode)
            {
                lastDateInBook = NextThirdWeds(lastDateInBook);
                var sparseDates = curveObj.PillarDates.Where(x => x <= lastDateInBook && DateExtensions.IsSparseLMEDate(x, curveObj.BuildDate, calendars)).ToArray();
                bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook, sparseDates);
                bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook, sparseDates) : null;
            }
            else
            {
                bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);
                bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook) : null;
            }


            ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
            {
                using var newVanillaModel = model.Clone();
                newVanillaModel.AddPriceCurve(curveName, bCurve.Value);

                var dependentCurves = new List<string>(linkedCurves);
                while (dependentCurves.Any())
                {
                    var newBaseCurves = new List<string>();
                    foreach (var depCurveName in dependentCurves)
                    {
                        var baseCurve = newVanillaModel.GetPriceCurve(depCurveName);
                        var recalCurve = ((BasisPriceCurve)newVanillaModel.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                        newVanillaModel.AddPriceCurve(depCurveName, recalCurve);
                        newBaseCurves.Add(depCurveName);
                    }

                    dependentCurves = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                }
                using var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                newPvModel.Dispose();

                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                if (computeGamma)
                {
                    using var newVanillaModelDown = model.Clone();
                    newVanillaModelDown.AddPriceCurve(curveName, bumpedDownCurves[bCurve.Key]);

                    var dependentCurvesDown = new List<string>(linkedCurves);
                    while (dependentCurvesDown.Any())
                    {
                        var newBaseCurves = new List<string>();
                        foreach (var depCurveName in dependentCurves)
                        {
                            var baseCurve = newVanillaModelDown.GetPriceCurve(depCurveName);
                            var recalCurve = ((BasisPriceCurve)newVanillaModelDown.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                            newVanillaModelDown.AddPriceCurve(depCurveName, recalCurve);
                            newBaseCurves.Add(depCurveName);
                        }

                        dependentCurvesDown = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                    }
                    using var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                    var bumpedPVCubeDown = newPvModelDown.PV(curveObj.Currency);
                    newPvModelDown.Dispose();
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    if (computeGamma)
                    {
                        var deltaUp = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize;
                        var deltaDown = (pvRows[i].Value - bumpedRowsDown[i].Value) / bumpSize;
                        var delta = (deltaUp + deltaDown) / 2.0;
                        var gamma = (deltaUp - deltaDown) / bumpSize;

                        if (Abs(delta) > 1e-8)
                        {
                            if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Delta" },
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, delta);
                        }

                        if (Abs(gamma) > 1e-8)
                        {
                            if (bCurve.Value.UnderlyingsAreForwards) //de-discount gamma
                                gamma /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Gamma" },
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, gamma);
                        }
                    }
                    else
                    {
                        var delta = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize;

                        if (delta != 0.0)
                        {
                            if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Delta" },
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, delta);
                        }
                    }
                }
            }, !parallelize).Wait();

            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }

        private static DateTime NextThirdWeds(DateTime date)
        {
            var w3 = date.ThirdWednesday();
            if (date > w3)
                return date.AddMonths(1).ThirdWednesday();
            else
                return w3;
        }

        public static ICube AssetDelta(this IPvModel pvModel, bool computeGamma = false, bool parallelize = false, DateTime[] pointsToBump = null, bool isSparseLMEMode = false, ICalendarProvider calendars = null, double bumpSize = 0.01)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "CurveType", typeof(string) },
                { "RefPrice", typeof(double) },
                { Consts.Cubes.Portfolio, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);

            cube.Initialize(dataTypes);
            using var model = pvModel.VanillaModel.Clone();
            model.BuildDependencyTree();

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);
                var bumpForCurve = bumpSize / 10 * curveObj.GetPriceForDate(model.BuildDate);
                var linkedCurves = model.GetDependentCurves(curveName);
                var allLinkedCurves = model.GetAllDependentCurves(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) &&
                    (ia.AssetIds.Contains(curveObj.AssetId) || ia.AssetIds.Any(aid => allLinkedCurves.Contains(aid))))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);
                
                Dictionary<string, IPriceCurve> bumpedCurves;
                Dictionary<string, IPriceCurve> bumpedDownCurves;
                if (isSparseLMEMode && curveObj is BasicPriceCurve bpc && bpc.CurveType == Transport.BasicTypes.PriceCurveType.LME)
                {
                    lastDateInBook = NextThirdWeds(lastDateInBook);
                    var sparseDates = curveObj.PillarDates.Where(x => x <= lastDateInBook && DateExtensions.IsSparseLMEDate(x, curveObj.BuildDate, calendars)).ToArray();
                    bumpedCurves = curveObj.GetDeltaScenarios(bumpForCurve, lastDateInBook, sparseDates);
                    bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpForCurve, lastDateInBook, sparseDates) : null;
                }
                else
                {
                    bumpedCurves = curveObj.GetDeltaScenarios(bumpForCurve, lastDateInBook, pointsToBump);
                    bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpForCurve, lastDateInBook, pointsToBump) : null;
                }

                ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
                {
                    using var newVanillaModel = model.Clone();
                    newVanillaModel.AddPriceCurve(curveName, bCurve.Value);

                    var dependentCurves = new List<string>(linkedCurves);
                    while (dependentCurves.Any())
                    {
                        var newBaseCurves = new List<string>();
                        foreach (var depCurveName in dependentCurves)
                        {
                            var baseCurve = newVanillaModel.GetPriceCurve(curveName);
                            var recalCurve = ((BasisPriceCurve)newVanillaModel.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                            newVanillaModel.AddPriceCurve(depCurveName, recalCurve);
                            newBaseCurves.Add(depCurveName);
                        }

                        dependentCurves = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                    }
                    using var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                    var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    ResultCubeRow[] bumpedRowsDown = null;
                    if (computeGamma)
                    {
                        using var newVanillaModelDown = model.Clone();
                        newVanillaModelDown.AddPriceCurve(curveName, bumpedDownCurves[bCurve.Key]);

                        var dependentCurvesDown = new List<string>(linkedCurves);
                        while (dependentCurvesDown.Any())
                        {
                            var newBaseCurves = new List<string>();
                            foreach (var depCurveName in dependentCurves)
                            {
                                var baseCurve = newVanillaModelDown.GetPriceCurve(depCurveName);
                                var recalCurve = ((BasisPriceCurve)newVanillaModelDown.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                                newVanillaModelDown.AddPriceCurve(depCurveName, recalCurve);
                                newBaseCurves.Add(depCurveName);
                            }

                            dependentCurvesDown = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                        }
                        using var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                        var bumpedPVCubeDown = newPvModelDown.PV(curveObj.Currency);
                        bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                        if (bumpedRowsDown.Length != pvRows.Length)
                            throw new Exception("Dimensions do not match");
                    }

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {

                        if (computeGamma)
                        {
                            var deltaUp = (bumpedRows[i].Value - pvRows[i].Value) / bumpForCurve;
                            var deltaDown = (pvRows[i].Value - bumpedRowsDown[i].Value) / bumpForCurve;
                            var delta = (deltaUp + deltaDown) / 2.0;
                            var gamma = (deltaUp - deltaDown) / bumpForCurve;

                            if (Abs(delta) > 1e-10)
                            {
                                if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                    delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, bumpedRows[i].MetaData[tidIx] },
                                    { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                    { PointLabel, bCurve.Key },
                                    { Metric, "Delta" },
                                    { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
                                    { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx] },
                                };
                                if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                                {
                                    foreach (var key in metaKeys)
                                    {
                                        if (trade.MetaData.TryGetValue(key, out var metaData))
                                            row[key] = metaData;
                                    }
                                }
                                cube.AddRow(row, delta);
                            }

                            if (Abs(gamma) > 1e-10)
                            {
                                //if (bCurve.Value.UnderlyingsAreForwards) //de-discount gamma
                                //    gamma /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, bumpedRows[i].MetaData[tidIx] },
                                    { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                    { PointLabel, bCurve.Key },
                                    { Metric, "Gamma" },
                                    { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
                                    { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx] },
                                };
                                if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                                {
                                    foreach (var key in metaKeys)
                                    {
                                        if (trade.MetaData.TryGetValue(key, out var metaData))
                                            row[key] = metaData;
                                    }
                                }
                                cube.AddRow(row, gamma);
                            }
                        }
                        else
                        {
                            var delta = (bumpedRows[i].Value - pvRows[i].Value) / bumpForCurve;

                            if (delta != 0.0)
                            {
                                if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                    delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, bumpedRows[i].MetaData[tidIx] },
                                    { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                    { PointLabel, bCurve.Key },
                                    { Metric, "Delta" },
                                    { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
                                    { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx] },
                                };
                                if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                                {
                                    foreach (var key in metaKeys)
                                    {
                                        if (trade.MetaData.TryGetValue(key, out var metaData))
                                            row[key] = metaData;
                                    }
                                }
                                cube.AddRow(row, delta);
                            }
                        }
                    }
                }, !(parallelize)).Wait();
            }
            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }

        public static ICube AssetDeltaStableGamma(this IPvModel pvModel, bool parallelize = false,
            DateTime[] pointsToBump = null, bool isSparseLMEMode = false, ICalendarProvider calendars = null,
            double bumpSize = 0.01, int nGammaPoints = 2)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "CurveType", typeof(string) },
                { "RefPrice", typeof(double) },
                { Consts.Cubes.Portfolio, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys)
                .Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }

            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null)
                .ToDictionary(x => x.TradeId, x => x);

            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel.Clone();
            model.BuildDependencyTree();

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);
                var bumpForCurve = bumpSize / 10 * curveObj.GetPriceForDate(model.BuildDate);
                var linkedCurves = model.GetDependentCurves(curveName);
                var allLinkedCurves = model.GetAllDependentCurves(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                        .Where(x => (x is IAssetInstrument ia) &&
                                    (ia.AssetIds.Contains(curveObj.AssetId) ||
                                     ia.AssetIds.Any(aid => allLinkedCurves.Contains(aid))))
                        .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var bumpedCurves = new Dictionary<string, IPriceCurve>[nGammaPoints];
                var bumpedDownCurves = new Dictionary<string, IPriceCurve>[nGammaPoints];
                if (isSparseLMEMode)
                {
                    lastDateInBook = NextThirdWeds(lastDateInBook);
                    var sparseDates = curveObj.PillarDates.Where(x =>
                            x <= lastDateInBook && DateExtensions.IsSparseLMEDate(x, curveObj.BuildDate, calendars))
                        .ToArray();
                    for (var i = 0; i < nGammaPoints; i++)
                    {
                        bumpedCurves[i] =
                            curveObj.GetDeltaScenarios(bumpForCurve * (i + 1), lastDateInBook, sparseDates);
                        bumpedDownCurves[i] =
                            curveObj.GetDeltaScenarios(-bumpForCurve * (i + 1), lastDateInBook, sparseDates);
                    }
                }
                else
                {
                    for (var i = 0; i < nGammaPoints; i++)
                    {
                        bumpedCurves[i] =
                            curveObj.GetDeltaScenarios(bumpForCurve * (i + 1), lastDateInBook, pointsToBump);
                        bumpedDownCurves[i] =
                            curveObj.GetDeltaScenarios(-bumpForCurve * (i + 1), lastDateInBook, pointsToBump);
                    }
                }

                var bumpedPvs = new Dictionary<string, ICube>[nGammaPoints];
                var bumpedDownPvs = new Dictionary<string, ICube>[nGammaPoints];

                for (var i = 0; i < nGammaPoints; i++)
                {
                    bumpedPvs[i] = [];
                    bumpedDownPvs[i] = [];

                    ParallelUtils.Instance.Foreach(bumpedCurves[i].ToList(), bCurve =>
                    {
                        var newVanillaModel = model.Clone();
                        newVanillaModel.AddPriceCurve(curveName, bCurve.Value);

                        var dependentCurves = new List<string>(linkedCurves);
                        while (dependentCurves.Any())
                        {
                            var newBaseCurves = new List<string>();
                            foreach (var depCurveName in dependentCurves)
                            {
                                var baseCurve = newVanillaModel.GetPriceCurve(curveName);
                                var recalCurve =
                                    ((BasisPriceCurve)newVanillaModel.GetPriceCurve(depCurveName)).ReCalibrate(
                                        baseCurve);
                                newVanillaModel.AddPriceCurve(depCurveName, recalCurve);
                                newBaseCurves.Add(depCurveName);
                            }

                            dependentCurves = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct()
                                .ToList();
                        }

                        var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);
                        bumpedPvs[i][bCurve.Key] = newPvModel.PV(curveObj.Currency);

                        var newVanillaModelDown = model.Clone();
                        newVanillaModelDown.AddPriceCurve(curveName, bumpedDownCurves[i][bCurve.Key]);

                        var dependentCurvesDown = new List<string>(linkedCurves);
                        while (dependentCurvesDown.Any())
                        {
                            var newBaseCurves = new List<string>();
                            foreach (var depCurveName in dependentCurves)
                            {
                                var baseCurve = newVanillaModelDown.GetPriceCurve(depCurveName);
                                var recalCurve = ((BasisPriceCurve)newVanillaModelDown.GetPriceCurve(depCurveName))
                                    .ReCalibrate(baseCurve);
                                newVanillaModelDown.AddPriceCurve(depCurveName, recalCurve);
                                newBaseCurves.Add(depCurveName);
                            }

                            dependentCurvesDown = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct()
                                .ToList();
                        }

                        var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                        bumpedDownPvs[i][bCurve.Key] = newPvModelDown.PV(curveObj.Currency);

                    }, !(parallelize)).Wait();
                }

                //by gamma point, trade row and then string point label
                
                var deltas = new Dictionary<string, double[]>[pvRows.Length];
                for (var n = 0; n < nGammaPoints; n++) //2gamma points, 5 pvs, 4 deltas
                {
                    foreach (var kv in bumpedCurves[0])
                    {
                        var innerRowsUp = n == 0 ? pvRows : bumpedPvs[n - 1][kv.Key].GetAllRows();
                        var innerRowsDown = n == 0 ? pvRows : bumpedDownPvs[n - 1][kv.Key].GetAllRows();

                        var outerRowsUp = bumpedPvs[n][kv.Key].GetAllRows();
                        var outerRowsDown = bumpedDownPvs[n][kv.Key].GetAllRows();
                        for (var i = 0; i < pvRows.Length; i++)
                        {
                            if (deltas[i] == null)
                                deltas[i] = new Dictionary<string, double[]>();
                            if (!deltas[i].TryGetValue(kv.Key, out var deltasForPoint))
                            {
                                deltasForPoint = new double[nGammaPoints * 2];
                                deltas[i][kv.Key] = deltasForPoint;
                            }

                            var pvInnerUp = innerRowsUp[i].Value;
                            var pvInnerDown = innerRowsDown[i].Value;
                            var pvOuterUp = outerRowsUp[i].Value;
                            var pvOuterDown = outerRowsDown[i].Value;
                            var deltaUp = (pvOuterUp - pvInnerUp) / bumpForCurve;
                            var deltaDown = (pvInnerDown - pvOuterDown) / bumpForCurve;

                            if (n == 0)
                            {
                                var delta = (deltaUp + deltaDown) / 2;

                                if (curveObj.UnderlyingsAreForwards) //de-discount delta
                                    delta /= GetUsdDF(model, (BasicPriceCurve)curveObj, curveObj.PillarDatesForLabel(kv.Key));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, pvRows[i].MetaData[tidIx] },
                                    { TradeType, pvRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", curveObj.PillarDatesForLabel(kv.Key) },
                                    { PointLabel, kv.Key },
                                    { Metric, "Delta" },
                                    { "CurveType", curveObj is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", curveObj.GetPriceForDate(curveObj.PillarDatesForLabel(kv.Key)) },
                                    { Consts.Cubes.Portfolio, pvRows[i].MetaData[pfIx] },
                                };
                                cube.AddRow(row, delta);
                            }

                            deltasForPoint[(nGammaPoints-1) + (n+1)] = deltaUp;
                            deltasForPoint[(nGammaPoints - 1) - n] = deltaDown;

                        }
                    }
                }

                var Xs = new double[nGammaPoints * 2];
                for (var i = 0; i < Xs.Length; i++)
                {
                    Xs[i] = (-nGammaPoints * 2 + 1 + i * 2) * bumpForCurve / 2;
                }
                //now we have the deltas, time for some regression
                for (var i = 0; i < pvRows.Length; i++)
                {
                    foreach (var kv in deltas[i])
                    {
                        var pointLabel = kv.Key;
                        var Ys = kv.Value;
                        var lr = LinearRegression.LinearRegressionNoVector(Xs, Ys, false);
                        var gamma = lr.Beta;
                        
                        var row = new Dictionary<string, object>
                        {
                            { TradeId, pvRows[i].MetaData[tidIx] },
                            { TradeType, pvRows[i].MetaData[tTypeIx] },
                            { AssetId, curveName },
                            { "PointDate", curveObj.PillarDatesForLabel(kv.Key) },
                            { PointLabel, kv.Key },
                            { Metric, "Gamma" },
                            { "CurveType", curveObj is BasisPriceCurve ? "Basis" : "Outright" },
                            { "RefPrice", curveObj.GetPriceForDate(curveObj.PillarDatesForLabel(kv.Key)) },
                            { Consts.Cubes.Portfolio, pvRows[i].MetaData[pfIx] },
                        };
                        cube.AddRow(row, gamma);
                    }    
                }
                
            }

            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }


        public static ICube AssetCashDelta(this IPvModel pvModel, Currency reportingCurrency = null)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) },
                { "CurveType", typeof(string) },
                { "Currency", typeof(string) },
                { Consts.Cubes.Portfolio, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel.Clone();
            model.BuildDependencyTree();

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);
                var linkedCurves = model.GetDependentCurves(curveName);
                var allLinkedCurves = model.GetAllDependentCurves(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) &&
                    (ia.AssetIds.Contains(curveObj.AssetId) || ia.AssetIds.Any(aid => allLinkedCurves.Contains(aid))))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddPriceCurve(curveName, bCurve.Value);

                    var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                    var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize;

                        if (delta != 0.0)
                        {
                            if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                            var date = bCurve.Value.PillarDatesForLabel(bCurve.Key);
                            var fwd = bCurve.Value.GetPriceForDate(date);
                            delta *= fwd;
                            if (reportingCurrency != null && bCurve.Value.Currency != reportingCurrency)
                            {
                                var fxFwd = model.FundingModel.GetFxRate(date, bCurve.Value.Currency, reportingCurrency);
                                delta *= fxFwd;
                            }
                            var row = new Dictionary<string, object>
                                {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { "PointDate", date },
                                { PointLabel, bCurve.Key },
                                { Metric, "Delta" },
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                { "Currency", reportingCurrency?.Ccy??bCurve.Value.Currency.Ccy },
                                { Consts.Cubes.Portfolio, bumpedRows[i].MetaData[pfIx] },
                                };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, delta);
                        }
                    }
                }, false).Wait();
            }
            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }


        public static ICube AssetParallelDelta(this IPvModel pvModel, ICurrencyProvider currencyProvider, double bumpSize = 0.01, string assetId = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var assetIds = pvModel.Portfolio.AssetIds();

            foreach (var curveName in assetIds)
            {
                if (assetId != null && curveName != assetId)
                    continue;

                var curveObj = model.GetPriceCurve(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) &&
                    ia.AssetIds.Contains(curveName))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                IPriceCurve bumpedCurve = curveObj switch
                {
                    ConstantPriceCurve con => new ConstantPriceCurve(con.Price + bumpSize, con.BuildDate, currencyProvider),
                    ContangoPriceCurve cpc => new ContangoPriceCurve(cpc.BuildDate, cpc.Spot + bumpSize, cpc.SpotDate, cpc.PillarDates, cpc.Contangos, currencyProvider, cpc.Basis, cpc.PillarLabels)
                    {
                        Currency = cpc.Currency,
                        AssetId = AssetId,
                        SpotCalendar = cpc.SpotCalendar,
                        SpotLag = cpc.SpotLag
                    },

                    // if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                    //delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                    BasicPriceCurve pc => new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p, ix) => p + (pc.UnderlyingsAreForwards ? (bumpSize / GetUsdDF(model, pc, pc.PillarDates[ix])) : bumpSize)).ToArray(), pc.CurveType, currencyProvider, pc.PillarLabels)
                    //BasicPriceCurve pc => new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p,ix) => p + bumpSize).ToArray(), pc.CurveType, currencyProvider, pc.PillarLabels)
                    {
                        CollateralSpec = pc.CollateralSpec,
                        Currency = pc.Currency,
                        AssetId = AssetId,
                        SpotCalendar = pc.SpotCalendar,
                        SpotLag = pc.SpotLag
                    },

                    EquityPriceCurve eqc => new EquityPriceCurve(eqc.BuildDate, eqc.Spot + bumpSize, eqc.Currency?.Ccy, eqc.IrCurve, eqc.SpotDate, currencyProvider)
                    {
                        Currency = eqc.Currency,
                        AssetId = AssetId,
                        SpotCalendar = eqc.SpotCalendar,
                        SpotLag = eqc.SpotLag
                    },

                    _ => throw new Exception("Unable to handle curve type for flat shift"),
                };
                var newVanillaModel = model.Clone();
                newVanillaModel.AddPriceCurve(curveName, bumpedCurve);

                using var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize;

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { Metric, "ParallelDelta" },
                            };
                        if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, delta);
                    }
                }
            }

            return cube.Sort(new List<string> { AssetId, TradeId });
        }

        private static IPriceCurve GetBumpedCurve(double bumpSize, IPriceCurve curveObjX, IAssetFxModel model, ICurrencyProvider currencyProvider)
        {
            return curveObjX switch
            {
                ConstantPriceCurve con => new ConstantPriceCurve(con.Price + bumpSize, con.BuildDate, currencyProvider),
                ContangoPriceCurve cpc => new ContangoPriceCurve(cpc.BuildDate, cpc.Spot + bumpSize, cpc.SpotDate, cpc.PillarDates, cpc.Contangos, currencyProvider, cpc.Basis, cpc.PillarLabels)
                {
                    Currency = cpc.Currency,
                    AssetId = AssetId,
                    SpotCalendar = cpc.SpotCalendar,
                    SpotLag = cpc.SpotLag,
                    Units = cpc.Units,
                },

                EquityPriceCurve epc => new EquityPriceCurve(epc.BuildDate, epc.Spot + bumpSize, epc.Currency?.Ccy, epc.IrCurve,epc.SpotDate, currencyProvider)
                {
                    Currency = epc.Currency,
                    AssetId = AssetId,
                    SpotCalendar = epc.SpotCalendar,
                    SpotLag = epc.SpotLag,
                    Units = epc.Units,
                },

                // if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                //delta /= GetUsdDF(model, (BasicPriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                BasicPriceCurve pc => new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p, ix) => p + (pc.UnderlyingsAreForwards ? (bumpSize / GetUsdDF(model, pc, pc.PillarDates[ix])) : bumpSize)).ToArray(), pc.CurveType, currencyProvider, pc.PillarLabels)
                //BasicPriceCurve pc => new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p,ix) => p + bumpSize).ToArray(), pc.CurveType, currencyProvider, pc.PillarLabels)
                {
                    CollateralSpec = pc.CollateralSpec,
                    Currency = pc.Currency,
                    AssetId = AssetId,
                    SpotCalendar = pc.SpotCalendar,
                    SpotLag = pc.SpotLag,
                    RefDate = pc.RefDate,
                    Units = pc.Units,
                },
                _ => throw new Exception("Unable to handle curve type for flat shift"),
            };
        }


        public static ICube AssetParallelDeltaGamma(this IPvModel pvModel, ICurrencyProvider currencyProvider, double bumpSize = 0.01, string assetId = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var assetIds = pvModel.Portfolio.AssetIds();

            foreach (var curveName in assetIds)
            {
                if (assetId != null && curveName != assetId)
                    continue;

                var curveObj = model.GetPriceCurve(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) &&
                    ia.AssetIds.Contains(curveName))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var bumpForCurve = bumpSize / 10 * curveObj.GetPriceForDate(model.BuildDate);

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);


                var bumpedCurveUp = GetBumpedCurve(bumpForCurve, curveObj, model.VanillaModel, currencyProvider);
                var bumpedCurveDown = GetBumpedCurve(-bumpForCurve, curveObj, model.VanillaModel, currencyProvider);

                var newVanillaModelUp = model.Clone();
                newVanillaModelUp.AddPriceCurve(curveName, bumpedCurveUp);

                var newVanillaModelDown = model.Clone();
                newVanillaModelDown.AddPriceCurve(curveName, bumpedCurveDown);

                using var newPvModelUp = pvModel.Rebuild(newVanillaModelUp, subPortfolio);
                using var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                var bumpedPVCubeUp = newPvModelUp.PV(curveObj.Currency);
                var bumpedPVCubeDown = newPvModelDown.PV(curveObj.Currency);

                var bumpedRowsUp = bumpedPVCubeUp.GetAllRows();
                var bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                if (bumpedRowsUp.Length != pvRows.Length || bumpedRowsDown.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                for (var i = 0; i < pvRows.Length; i++)
                {
                    var deltaUp = (bumpedRowsUp[i].Value - pvRows[i].Value) / bumpForCurve;
                    var deltaDown = (pvRows[i].Value - bumpedRowsDown[i].Value) / bumpForCurve;
                    var delta = (deltaUp + deltaDown) / 2;
                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                            {
                                { TradeId, pvRows[i].MetaData[tidIx] },
                                { TradeType, pvRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { Metric, "ParallelDelta" },
                            };
                        if (insDict.TryGetValue((string)pvRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, delta);
                    }

                    var gamma = (deltaUp - deltaDown) / bumpForCurve;
                    if (gamma != 0.0)
                    {
                        var row = new Dictionary<string, object>
                            {
                                { TradeId, pvRows[i].MetaData[tidIx] },
                                { TradeType, pvRows[i].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { Metric, "ParallelGamma" },
                            };
                        if (insDict.TryGetValue((string)pvRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, gamma);
                    }
                }
            }

            return cube.Sort(new List<string> { AssetId, TradeId });
        }

        public static ICube FxDelta(this IPvModel pvModel, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false, bool reportInverseDelta = false)
        {
            var bumpSize = 0.001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
                { "Portfolio", typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var mf = model.FundingModel.DeepClone(null);

            var domCcy = model.FundingModel.FxMatrix.BaseCurrency;

            if (homeCcy != null && homeCcy != domCcy)//remap onto new base currency
            {
                domCcy = homeCcy;
                mf = FundingModel.RemapBaseCurrency(mf, homeCcy, currencyProvider);
            }

            var m = model.Clone(mf);

            foreach (var currency in m.FundingModel.FxMatrix.SpotRates.Keys)
            {
                using var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
                var pvCube = newPvModel.PV(m.FundingModel.FxMatrix.BaseCurrency);
                
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex("Portfolio");

                var fxPair = $"{domCcy}/{currency}";
                //var fxPair = $"{currency}/{domCcy}";

                var newModel = m.Clone();
                var baseSpot = m.FundingModel.FxMatrix.SpotRates[currency];
                var bumpedSpot = baseSpot * (1.00 + bumpSize);
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var spotBump = reportInverseDelta ? (1 / bumpedSpot - 1 / baseSpot) : (bumpedSpot - baseSpot);
                using var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
                var bumpedPVCube = bumpedPvModel.PV(m.FundingModel.FxMatrix.BaseCurrency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                var spotBumpDown = 0.0;

                var dfToSpotDate = m.FundingModel.GetDf(m.FundingModel.FxMatrix.BaseCurrency, m.BuildDate, m.FundingModel.FxMatrix.GetFxPair(fxPair).SpotDate(m.BuildDate));

                if (computeGamma)
                {
                    var bumpedSpotDown = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 - bumpSize);
                    newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpotDown;
                    spotBumpDown = reportInverseDelta ? (1 / bumpedSpotDown - 1 / baseSpot) : (bumpedSpotDown - baseSpot);

                    var bumpedPvModelDown = pvModel.Rebuild(newModel, pvModel.Portfolio);

                    var bumpedPVCubeDown = bumpedPvModelDown.PV(m.FundingModel.FxMatrix.BaseCurrency);
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var pnl = bumpedRows[i].Value - pvRows[i].Value;
                    var delta = (reportInverseDelta ? -1 : 1) * pnl / spotBump / dfToSpotDate; 

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                        {
                            { TradeId, bumpedRows[i].MetaData[tidIx] },
                            { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                            { AssetId, fxPair },
                            { Metric, "FxSpotDelta" },
                            { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                        };
                        if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, delta);
                    }

                    if (computeGamma)
                    {
                        var noGammaTypes = new[] { "Equity", "Bond", "Cash", "Future", "LoanDepo", "FxForward" };
                        var tradeType = bumpedRows[i].MetaData[tTypeIx];
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / spotBumpDown / dfToSpotDate;
                        var gamma = (delta - deltaDown) / (spotBump - spotBumpDown) * 2.0;
                        if (gamma != 0.0 && !noGammaTypes.Contains(tradeType))
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, tradeType },
                                { AssetId, fxPair },
                                { Metric, "FxSpotGamma" },
                                { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, gamma);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube FxDeltaRaw(this IPvModel pvModel, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false)
        {
            var bumpSize = 0.0001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
                { "Portfolio", typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var mf = model.FundingModel.DeepClone(null);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var m = model.Clone(mf);

            foreach (var currency in m.FundingModel.FxMatrix.SpotRates.Keys)
            {
                var homeToCcyPair = mf.FxMatrix.GetFxPair(homeCcy, currency);
                var homeToCcyRate = mf.GetFxRate(homeToCcyPair.SpotDate(model.BuildDate), homeCcy, currency);

                using var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
                var pvCube = newPvModel.PV(homeCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex("Portfolio");

                //var fxPair = $"{domCcy}/{currency}";
                var fxPair = $"{currency}/{baseCcy}";

                var newModel = m.Clone();
                var bumpedSpot = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 + bumpSize);
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var spotBump = bumpedSpot - m.FundingModel.FxMatrix.SpotRates[currency];
                using var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
                var bumpedPVCube = bumpedPvModel.PV(homeCcy);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                var spotBumpDown = 0.0;

                var dfToSpotDate = m.FundingModel.GetDf(m.FundingModel.FxMatrix.BaseCurrency, m.BuildDate, m.FundingModel.FxMatrix.GetFxPair(fxPair).SpotDate(m.BuildDate));

                if (computeGamma)
                {
                    var bumpedSpotDown = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 - bumpSize);
                    newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpotDown;
                    spotBumpDown = bumpedSpotDown - m.FundingModel.FxMatrix.SpotRates[currency];

                    var bumpedPvModelDown = pvModel.Rebuild(newModel, pvModel.Portfolio);

                    var bumpedPVCubeDown = bumpedPvModelDown.PV(homeCcy);
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / spotBump / dfToSpotDate * homeToCcyRate;

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                        {
                            { TradeId, bumpedRows[i].MetaData[tidIx] },
                            { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                            { AssetId, fxPair },
                            { Metric, "FxSpotDelta" },
                            { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                        };
                        if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, delta);
                    }

                    if (computeGamma)
                    {
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / spotBumpDown / dfToSpotDate * homeToCcyRate;
                        var gamma = (delta - deltaDown) / (spotBump - spotBumpDown) * 2.0;
                        if (gamma != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, fxPair },
                                { Metric, "FxSpotGamma" },
                                { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, gamma);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube FxDeltaSpecific(this IPvModel pvModel, Currency homeCcy, List<FxPair> pairsToBump, ICurrencyProvider currencyProvider, bool computeGamma = false)
        {
            var bumpSize = 0.0001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
                { "Portfolio", typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var mf = model.FundingModel.DeepClone(null);
            var baseCcy = model.FundingModel.FxMatrix.BaseCurrency;
            var m = model.Clone(mf);

            var homeBasePair = mf.FxMatrix.GetFxPair(homeCcy, baseCcy);
            var homeToBase = mf.GetFxRate(homeBasePair.SpotDate(model.BuildDate), homeCcy, baseCcy);

            ParallelUtils.Instance.Foreach(pairsToBump, pair =>
            //foreach (var pair in pairsToBump)
            {
                string fxPair;
                bool flipDelta;
                Currency currency;
                if (pair.Foreign == baseCcy)
                {
                    fxPair = $"{pair.Domestic}/{baseCcy}";
                    flipDelta = true;
                    currency = pair.Domestic;
                }
                else if (pair.Domestic == baseCcy)
                {
                    fxPair = $"{pair.Foreign}/{baseCcy}";
                    flipDelta = false;
                    currency = pair.Foreign;
                }
                else
                    throw new Exception("Pairs must contain base currency");

                using var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
                var pvCube = newPvModel.PV(homeCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex("Portfolio");

                var newModel = m.Clone();
                var bumpedSpot = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 + bumpSize);
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var spotBump = flipDelta ?
                    1.0 / bumpedSpot - 1.0 / m.FundingModel.FxMatrix.SpotRates[currency] :
                    (bumpedSpot - m.FundingModel.FxMatrix.SpotRates[currency]) * homeToBase;
                using var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
                var bumpedPVCube = bumpedPvModel.PV(homeCcy);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                var spotBumpDown = 0.0;

                var dfToSpotDate = m.FundingModel.GetDf(m.FundingModel.FxMatrix.BaseCurrency, m.BuildDate, m.FundingModel.FxMatrix.GetFxPair(fxPair).SpotDate(m.BuildDate));

                if (computeGamma)
                {
                    var bumpedSpotDown = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 - bumpSize);
                    newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpotDown;
                    spotBumpDown = bumpedSpotDown - m.FundingModel.FxMatrix.SpotRates[currency];

                    using var bumpedPvModelDown = pvModel.Rebuild(newModel, pvModel.Portfolio);

                    var bumpedPVCubeDown = bumpedPvModelDown.PV(homeCcy);
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / spotBump / dfToSpotDate * homeToBase;

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                        {
                            { TradeId, bumpedRows[i].MetaData[tidIx] },
                            { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                            { AssetId, pair.ToString() },
                            { Metric, "FxSpotDelta" },
                            { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                        };
                        if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                        {
                            foreach (var key in metaKeys)
                            {
                                if (trade.MetaData.TryGetValue(key, out var metaData))
                                    row[key] = metaData;
                            }
                        }
                        cube.AddRow(row, delta);
                    }

                    if (computeGamma)
                    {
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / spotBumpDown / dfToSpotDate * homeToBase;
                        var gamma = (delta - deltaDown) / (spotBump - spotBumpDown) * 2.0;
                        if (gamma != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, pair.ToString() },
                                { Metric, "FxSpotGamma" },
                                { "Portfolio", bumpedRows[i].MetaData[pfIx]  }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, gamma);
                        }
                    }
                }
            }, false).Wait();
            return cube.Sort();
        }


        public static ICube AssetIrDelta(this IPvModel pvModel, Currency reportingCcy = null, double bumpSize = 0.0001, bool paralellize = true)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            foreach (var curve in model.FundingModel.Curves)
            {
                var curveObj = curve.Value as IrCurve;

                if (curveObj == null)
                    continue;

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) && (ia.IrCurves(model).Contains(curve.Key) || (reportingCcy != null && reportingCcy != ia.Currency)))
                    .ToList()
                };
                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                using var subModel = pvModel.Rebuild(pvModel.VanillaModel, subPortfolio);
                var pvCube = subModel.PV(reportingCcy ?? curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                var bumpedCurves = curveObj.BumpScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
                //foreach(var bCurve in bumpedCurves.ToList())
                {
                    var newModel = model.Clone();
                    newModel.FundingModel.Curves[curve.Key] = bCurve.Value;
                    using var newPvModel = pvModel.Rebuild(newModel, subPortfolio);
                    var bumpedPVCube = newPvModel.PV(reportingCcy ?? curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = bumpedRows[i].Value - pvRows[i].Value;

                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, curve.Key },
                                { "PointDate", bCurve.Key},
                                { PointLabel, bCurve.Key.ToString("yyyy-MM-dd") },
                                { Metric, "IrDelta" }
                            };
                            if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, delta);
                        }
                    }
                },!paralellize).Wait();
            }

            return cube.Sort();
        }

        public static ICube AssetDeltaDecay(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider = null, bool useFv = false, bool isSparseLmeMode = false)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { PointLabel, typeof(string) },
                { PointDate, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            var pvCube = useFv ? pvModel.FV(reportingCcy) : pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            //today fixing drop-off
            var baseDeltaCube = pvModel.AssetDelta(isSparseLMEMode: isSparseLmeMode, calendars: calendarProvider);
            var vm = pvModel.VanillaModel;
            foreach (var fName in vm.FixingDictionaryNames)
            {
                var cName = fName.Split('¬')[0];
                var fixings = pvModel.VanillaModel.GetFixingDictionary(fName);
                if (!fixings.ContainsKey(model.BuildDate))
                {
                    var newDict = fixings.Clone();
                    if (!fixings.ContainsKey(model.BuildDate))
                    {
                        if (newDict.FixingDictionaryType == FixingDictionaryType.Asset)
                        {
                            var curve = model.GetPriceCurve(newDict.AssetId ?? (fName?.Split('¬').FirstOrDefault()));
                            var estFixing = curve.GetPriceForDate(model.BuildDate.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag));
                            newDict.Add(model.BuildDate, estFixing);
                        }
                        else //its FX
                        {
                            var id = newDict.FxPair ?? newDict.AssetId;
                            var ccyLeft = currencyProvider[id.Substring(0, 3)];
                            var ccyRight = currencyProvider[id.Substring(id.Length - 3, 3)];
                            var pair = model.FundingModel.FxMatrix.GetFxPair(ccyLeft, ccyRight);
                            var spotDate = pair.SpotDate(model.BuildDate);
                            var estFixing = model.FundingModel.GetFxRate(spotDate, ccyLeft, ccyRight);
                            newDict.Add(model.BuildDate, estFixing);
                        }

                        pvModel.VanillaModel.AddFixingDictionary(fName, newDict);
                    }
                }
            }
            using var clonedFix = pvModel.Rebuild(pvModel.VanillaModel, pvModel.Portfolio);
            var fpv = clonedFix.FV(reportingCcy);
            var fpp = pvModel.FV(reportingCcy);
            var fixedDeltaCube = clonedFix.AssetDelta(isSparseLMEMode: isSparseLmeMode, calendars: calendarProvider);
            var fixCube = fixedDeltaCube.Difference(baseDeltaCube, ["TradeId", "AssetId", "Metric", "PointLabel"]);
            var fixRows = fixCube.GetAllRows();
            var plId = fixCube.GetColumnIndex(PointLabel);
            var pdId = fixCube.GetColumnIndex(PointDate);
            var aId = fixCube.GetColumnIndex(AssetId);
            var ttId = fixCube.GetColumnIndex(TradeType);
            foreach (var fixRow in fixRows)
            {
                var row = new Dictionary<string, object>
                    {
                    { TradeId, fixRow.MetaData[tidIx] },
                    { TradeType, fixRow.MetaData[ttId] },
                    { AssetId, fixRow.MetaData[aId] },
                    { PointLabel, fixRow.MetaData[plId] },
                    { PointDate, fixRow.MetaData[pdId] },
                    { Metric, "FixingDelta" }
                    };
                if (insDict.TryGetValue((string)fixRow.MetaData[tidIx], out var trade))
                {
                    foreach (var key in metaKeys)
                    {
                        if (trade.MetaData.TryGetValue(key, out var metaData))
                            row[key] = metaData;
                    }
                }
                cube.AddRow(row, fixRow.Value);
            }


            //time-decay / charm
            var rolledPortfolio = pvModel.Portfolio.RollWithLifecycle(pvModel.VanillaModel.BuildDate, fwdValDate);
            using var cloned = clonedFix.Rebuild(clonedFix.VanillaModel, rolledPortfolio);

            using IPvModel rolledPvModel = (cloned is AssetFxMCModel amc) ?
                        amc.RollModel(fwdValDate, currencyProvider, null, calendarProvider) :
                        (cloned is AssetFxModel afx ? afx.RollModel(fwdValDate, currencyProvider) : throw new Exception("Unsupported model type"));

            var pvCubeFwd = useFv ? rolledPvModel.FV(reportingCcy) : rolledPvModel.PV(reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();



            var rolledDeltaCube = rolledPvModel.AssetDelta(isSparseLMEMode: isSparseLmeMode, calendars: calendarProvider);
            var charmCube = rolledDeltaCube.Difference(fixedDeltaCube, ["TradeId", "AssetId", "Metric", "PointLabel"]);
            var charmRows = charmCube.GetAllRows();
            foreach (var charmRow in charmRows)
            {
                var row = new Dictionary<string, object>
                    {
                    { TradeId, charmRow.MetaData[tidIx] },
                    { TradeType, charmRow.MetaData[ttId] },
                    { AssetId, charmRow.MetaData[aId] },
                    { PointLabel, charmRow.MetaData[plId] },
                    { PointDate, charmRow.MetaData[pdId] },
                    { Metric, "Charm" }
                    };
                if (insDict.TryGetValue((string)charmRow.MetaData[tidIx], out var trade))
                {
                    foreach (var key in metaKeys)
                    {
                        if (trade.MetaData.TryGetValue(key, out var metaData))
                            row[key] = metaData;
                    }
                }
                cube.AddRow(row, charmRow.Value);
            }

            return cube;
        }


        public static ICube AssetThetaCharm(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider, bool computeCharm = false, List<FxPair> FxPairsToRisk = null, ICalendarProvider calendarProvider = null, bool useFv = false, bool isSparseLmeMode = false)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            var pvCube = useFv ? pvModel.FV(reportingCcy) : pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = pvModel.Portfolio.FlowsT0(model, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);
          
            var rolledPortfolio = pvModel.Portfolio.RollWithLifecycle(pvModel.VanillaModel.BuildDate, fwdValDate);
            using var cloned = pvModel.Rebuild(model.VanillaModel, rolledPortfolio);
        
            //var rolledVanillaModel = model.RollModel(fwdValDate, currencyProvider);
             //using var rolledPvModel = pvModel.Rebuild(rolledVanillaModel, rolledPortfolio);

            using IPvModel rolledPvModel = (cloned is AssetFxMCModel amc) ?
                        amc.RollModel(fwdValDate, currencyProvider, null, calendarProvider) :
                        (cloned is AssetFxModel afx ? afx.RollModel(fwdValDate, currencyProvider) : throw new Exception("Unsupported model type"));

            var pvCubeFwd = useFv ? rolledPvModel.FV(reportingCcy) : rolledPvModel.PV(reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            //theta
            for (var i = 0; i < pvRowsFwd.Length; i++)
            {

                var theta = (string)cashRows[i].MetaData[tTypeIx] != "Physical" ? pvRowsFwd[i].Value - pvRows[i].Value : 0.0;

                if (theta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, pvRowsFwd[i].MetaData[tidIx] },
                        { TradeType, pvRowsFwd[i].MetaData[tTypeIx] },
                        { AssetId,  string.Empty },
                        { PointLabel, string.Empty },
                        { Metric, "Theta" }
                    };
                    if (insDict.TryGetValue((string)pvRowsFwd[i].MetaData[tidIx], out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                        if (trade is IAssetInstrument ia)
                            row[AssetId] = ia.AssetIds.FirstOrDefault() ?? string.Empty;
                    }
                    cube.AddRow(row, theta);
                }
            }

            //cash move
            for (var i = 0; i < cashRows.Length; i++)
            {
                var cash = cashRows[i].Value;
                if (cash != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, cashRows[i].MetaData[tidIx] },
                        { TradeType, cashRows[i].MetaData[tTypeIx] },
                        { AssetId, string.Empty },
                        { PointLabel, "CashMove" },
                        { Metric, "Theta" }
                    };
                    if (insDict.TryGetValue((string)cashRows[i].MetaData[tidIx], out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                        if (trade is IAssetInstrument ia)
                            row[AssetId] = ia.AssetIds.FirstOrDefault() ?? string.Empty;
                    }
                    cube.AddRow(row, cash);
                }
            }

            //charm-asset
            if (computeCharm)
            {
                var baseDeltaCube = pvModel.AssetDelta(isSparseLMEMode: isSparseLmeMode, calendars: calendarProvider);
                var rolledDeltaCube = rolledPvModel.AssetDelta(isSparseLMEMode: isSparseLmeMode, calendars: calendarProvider);
                var charmCube = rolledDeltaCube.Difference(baseDeltaCube, ["TradeId","AssetId","Metric","PointLabel"]);
                var charmRows = charmCube.GetAllRows();
                var plId = charmCube.GetColumnIndex(PointLabel);
                var aId = charmCube.GetColumnIndex(AssetId);
                var ttId = charmCube.GetColumnIndex(TradeType);
                foreach (var charmRow in charmRows)
                {
                    var row = new Dictionary<string, object>
                    {
                    { TradeId, charmRow.MetaData[tidIx] },
                    { TradeType, charmRow.MetaData[ttId] },
                    { AssetId, charmRow.MetaData[aId] },
                    { PointLabel, charmRow.MetaData[plId] },
                    { Metric, "Charm" }
                    };
                    if (insDict.TryGetValue((string)charmRow.MetaData[tidIx], out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                    }
                    cube.AddRow(row, charmRow.Value);
                }

                //charm-fx
                baseDeltaCube = FxPairsToRisk == null ? pvModel.FxDeltaRaw(reportingCcy, currencyProvider) : pvModel.FxDeltaSpecific(reportingCcy, FxPairsToRisk, currencyProvider);
                rolledDeltaCube = FxPairsToRisk == null ? rolledPvModel.FxDeltaRaw(reportingCcy, currencyProvider) : rolledPvModel.FxDeltaSpecific(reportingCcy, FxPairsToRisk, currencyProvider);
                charmCube = rolledDeltaCube.Difference(baseDeltaCube);
                var fId = charmCube.GetColumnIndex(AssetId);
                foreach (var charmRow in charmCube.GetAllRows())
                {
                    var row = new Dictionary<string, object>
                    {
                    { TradeId, charmRow.MetaData[tidIx] },
                    { AssetId, charmRow.MetaData[fId] },
                    { TradeType, charmRow.MetaData[ttId] },
                    { PointLabel, string.Empty },
                    { Metric, "Charm" }
                    };
                    if (insDict.TryGetValue((string)charmRow.MetaData[tidIx], out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                    }
                    cube.AddRow(row, charmRow.Value);
                }
            }
            return cube;
        }

        public static ICube AssetAnalyticTheta(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider, bool computeCharm = false)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { PointLabel, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            var thetasByTrade = pvModel.Portfolio.Instruments
                .Select(x => new KeyValuePair<IInstrument, (double Financing, double Option)>(x, x.Theta(model, fwdValDate, reportingCcy)))
                .ToArray();

            foreach (var t in thetasByTrade)
            {
                var finTheta = t.Value.Financing;
                if (finTheta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, t.Key.TradeId},
                        { TradeType, (t.Key as IAssetInstrument)?.TradeType()  },
                        { AssetId, string.Empty },
                        { PointLabel, "Financing" },
                        { Metric, "Theta" }
                    };
                    if (insDict.TryGetValue(t.Key.TradeId, out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                    }
                    cube.AddRow(row, finTheta);
                }

                var optTheta = t.Value.Option;
                if (optTheta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, t.Key.TradeId},
                        { TradeType, (t.Key as IAssetInstrument)?.TradeType()  },
                        { AssetId, string.Empty },
                        { PointLabel, "Option" },
                        { Metric, "Theta" }
                    };
                    if (insDict.TryGetValue(t.Key.TradeId, out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                    }
                    cube.AddRow(row, optTheta);
                }
            }

            return cube;
        }

        public static ICube CorrelationDelta(this IPvModel pvModel, Currency reportingCcy, double epsilon)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { Metric, typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var pvCube = pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex(TradeId);

            var bumpedCorrelMatrix = pvModel.VanillaModel.CorrelationMatrix.Bump(epsilon);

            var newVanillaModel = pvModel.VanillaModel.Clone();
            newVanillaModel.CorrelationMatrix = bumpedCorrelMatrix;
            var newPvModel = pvModel.Rebuild(newVanillaModel, pvModel.Portfolio);

            var bumpedPVCube = newPvModel.PV(reportingCcy);
            var bumpedRows = bumpedPVCube.GetAllRows();
            if (bumpedRows.Length != pvRows.Length)
                throw new Exception("Dimensions do not match");
            for (var i = 0; i < bumpedRows.Length; i++)
            {
                //flat bump of correlation matrix by single epsilon parameter, reported as PnL
                var cDelta = bumpedRows[i].Value - pvRows[i].Value;
                if (cDelta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { TradeId, bumpedRows[i].MetaData[tidIx] },
                        { Metric, "CorrelDelta" }
                    };
                    if (insDict.TryGetValue((string)bumpedRows[i].MetaData[tidIx], out var trade))
                    {
                        foreach (var key in metaKeys)
                        {
                            if (trade.MetaData.TryGetValue(key, out var metaData))
                                row[key] = metaData;
                        }
                    }
                    cube.AddRow(row, cDelta);
                }
            }

            return cube;
        }

        public static ICube ContangoSwapDelta(this IPvModel pvModel, ICurrencyProvider currencyProvider, string assetId = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { AssetId, typeof(string) },
                { Metric, typeof(string) },
                { PointLabel, typeof(string) },
                { PointDate, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);
            var model = pvModel.VanillaModel;

            var assetIds = pvModel.Portfolio.AssetIds();

            foreach (var curveName in assetIds)
            {
                if (assetId != null && curveName != assetId)
                    continue;

                if (model.GetPriceCurve(curveName) is not ContangoPriceCurve curveObj)
                    continue;

                var subPortfolio = new Portfolio()
                {
                    Instruments = pvModel.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) &&
                    ia.AssetIds.Contains(curveName))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                var bump = 0.001;

                for (var c = 0; c < curveObj.Contangos.Length; c++)
                {
                    var t = curveObj.BuildDate.CalculateYearFraction(curveObj.PillarDates[c], Transport.BasicTypes.DayCountBasis.ACT360);
                    var scale = curveObj.Spot * bump * t * model.FundingModel.GetDf(curveObj.Currency, curveObj.BuildDate, curveObj.PillarDates[c]);
                    var bumpedRates = curveObj.Contangos.Select((x, ix) => x + (ix == c ? bump : 0)).ToArray();
                    var newCurve = new ContangoPriceCurve(curveObj.BuildDate, curveObj.Spot, curveObj.SpotDate, curveObj.PillarDates, bumpedRates, currencyProvider, curveObj.Basis, curveObj.PillarLabels)
                    {
                        Currency = curveObj.Currency,
                        AssetId = AssetId,
                        SpotCalendar = curveObj.SpotCalendar,
                        SpotLag = curveObj.SpotLag
                    };

                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddPriceCurve(curveName, newCurve);
                    var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPvs = newPvModel.PV(curveObj.Currency).GetAllRows();

                    for (var ii = 0; ii < pvRows.Length; ii++)
                    {
                        var pvDiff = (bumpedPvs[ii].Value - pvRows[ii].Value);
                        var delta = pvDiff / scale;
                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, pvRows[ii].MetaData[tidIx] },
                                { TradeType, pvRows[ii].MetaData[tTypeIx] },
                                { AssetId, curveName },
                                { Metric, "ContangoSwapDelta" },
                                { PointDate, curveObj.PillarDates[c] },
                                { PointLabel, curveObj.PillarLabels[c] },
                            };
                            if (insDict.TryGetValue((string)pvRows[ii].MetaData[tidIx], out var trade))
                            {
                                foreach (var key in metaKeys)
                                {
                                    if (trade.MetaData.TryGetValue(key, out var metaData))
                                        row[key] = metaData;
                                }
                            }
                            cube.AddRow(row, delta);
                        }
                    }

                    if (curveObj.PillarDates[c] > lastDateInBook)
                        break;
                }
            }

            return cube.Sort(new List<string> { AssetId, TradeId });
        }


        public static async Task<ICube> AssetGreeks(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { PointLabel, typeof(string) },
                { "PointDate", typeof(DateTime) },
                { Metric, typeof(string) },
                { "Currency", typeof(string) },
                { "Portfolio", typeof(string) }
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var pvCube = pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = pvModel.Portfolio.FlowsT0(pvModel.VanillaModel, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            var rolledVanillaModel = pvModel.VanillaModel.RollModel(fwdValDate, currencyProvider);
            using var rolledPvModel = pvModel.Rebuild(rolledVanillaModel, pvModel.Portfolio);

            var pvCubeFwd = rolledPvModel.PV(reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            //theta
            var thetaCube = pvCubeFwd.QuickDifference(pvCube);
            cube = cube.Merge(thetaCube, new Dictionary<string, object>
            {
                 { AssetId, string.Empty },
                 { PointLabel, string.Empty },
                 { "PointDate", fwdValDate },
                 { Metric, "Theta" }
            });


            //cash move
            cube = cube.Merge(cashCube, new Dictionary<string, object>
            {
                 { AssetId, string.Empty },
                 { PointLabel, "CashMove"  },
                 { "PointDate", fwdValDate },
                 { Metric, "Theta" }
            });

            //setup to run in parallel
            var tasks = new Dictionary<string, Task<ICube>>
            {
                { "AssetDeltaGamma", new Task<ICube>(() => pvModel.AssetDelta(true)) },
                { "AssetVega", new Task<ICube>(() => rolledPvModel.AssetVega(reportingCcy)) },
                { "FxVega", new Task<ICube>(() => rolledPvModel.FxVega(reportingCcy)) },
                { "RolledDeltaGamma", new Task<ICube>(() => rolledPvModel.AssetDelta(true)) },
                { "FxDeltaGamma", new Task<ICube>(() => pvModel.FxDelta(reportingCcy, currencyProvider, true)) },
                { "RolledFxDeltaGamma", new Task<ICube>(() => rolledPvModel.FxDelta(reportingCcy, currencyProvider, true)) },
                { "IrDelta", new Task<ICube>(() => pvModel.AssetIrDelta(reportingCcy)) }
            };

            await ParallelUtils.Instance.QueueAndRunTasks(tasks.Values);


            //delta
            var baseDeltaGammaCube = tasks["AssetDeltaGamma"].Result;
            cube = cube.Merge(baseDeltaGammaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //vega
            var assetVegaCube = tasks["AssetVega"].Result;
            cube = cube.Merge(assetVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });
            //var fxVegaCube = FxVega(portfolio, model, reportingCcy);
            var fxVegaCube = tasks["FxVega"].Result;
            cube = cube.Merge(fxVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //charm-asset
            //var rolledDeltaGammaCube = AssetDeltaGamma(portfolio, rolledModel);
            var rolledDeltaGammaCube = tasks["RolledDeltaGamma"].Result;

            var baseDeltaCube = baseDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "Delta" } });
            var rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "Delta" } });
            var rolledGammaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "Gamma" } });
            var charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            cube = cube.Merge(charmCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
                 { Metric, "Charm"},
            });
            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { Metric, "AssetDeltaT1" },
            });
            cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { Metric, "AssetGammaT1" },
            });

            //charm-fx
            baseDeltaCube = tasks["FxDeltaGamma"].Result;
            cube = cube.Merge(baseDeltaCube, new Dictionary<string, object>
            {
                { "PointDate", DateTime.MinValue },
                { PointLabel, string.Empty },
                { "Currency", string.Empty },
            });
            rolledDeltaGammaCube = tasks["RolledFxDeltaGamma"].Result;
            rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "FxSpotDelta" } });
            rolledGammaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "FxSpotGamma" } });

            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { PointLabel, string.Empty },
                 { "PointDate", DateTime.MinValue },
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { Metric, "FxSpotDeltaT1" },
            });

            cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
            {
                 { PointLabel, string.Empty },
                 { "PointDate", DateTime.MinValue },
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { Metric, "FxSpotGammaT1" },
            });

            charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var fId = charmCube.GetColumnIndex(AssetId);
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { TradeId, charmRow.MetaData[tidIx] },
                    { TradeType, charmRow.MetaData[tTypeIx] },
                    { AssetId, charmRow.MetaData[fId] },
                    { PointLabel, string.Empty },
                    { "PointDate", DateTime.MinValue },
                    { "Currency", string.Empty },
                    { Metric, "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            //ir-delta
            var baseIrDeltacube = tasks["IrDelta"].Result;
            cube = cube.Merge(baseIrDeltacube, new Dictionary<string, object>
            {
                 { "Currency", reportingCcy.Ccy },
            });

            return cube;
        }

        public static ICube AssetGreeksSafe(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType, typeof(string) },
                { AssetId, typeof(string) },
                { PointLabel, typeof(string) },
                { PointDate, typeof(DateTime) },
                { Metric, typeof(string) },
                { "Currency", typeof(string) },
                { "Portfolio", typeof(string) },
                { Underlying, typeof(string) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach (var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).ToDictionary(x => x.TradeId, x => x);
            cube.Initialize(dataTypes);

            var pvCube = pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = pvModel.Portfolio.FlowsT0(pvModel.VanillaModel, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            var rolledVanillaModel = pvModel.VanillaModel.RollModel(fwdValDate, currencyProvider);
            using  var rolledPvModel = pvModel.Rebuild(rolledVanillaModel, pvModel.Portfolio);

            var pvCubeFwd = rolledPvModel.PV(reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            //theta
            var thetaCube = pvCubeFwd.QuickDifference(pvCube);
            cube = cube.Merge(thetaCube, new Dictionary<string, object>
            {
                 { AssetId, string.Empty },
                 { PointLabel, string.Empty },
                 { PointDate, fwdValDate },
                 { Metric, "Theta" }
            }, mergeTypes: true);


            //cash move
            cube = cube.Merge(cashCube, new Dictionary<string, object>
            {
                 { AssetId, string.Empty },
                 { PointLabel, "CashMove"  },
                 { PointDate, fwdValDate },
                 { Metric, "Theta" }
            }, mergeTypes: true);

            //setup and run in series
            var tasks = new Dictionary<string, ICube>();

            tasks["AssetDeltaGamma"] = pvModel.AssetDelta(false, isSparseLMEMode: true, calendars: calendarProvider);
            tasks["AssetVega"] = rolledPvModel.AssetVega(reportingCcy);
            //tasks["FxVega"] = rolledPvModel.FxVega(reportingCcy);
            tasks["RolledParallelGamma"] = rolledPvModel.AssetParallelDeltaGamma(currencyProvider);
            tasks["RolledDeltaGamma"] = rolledPvModel.AssetDelta(false, isSparseLMEMode:true, calendars: calendarProvider, bumpSize: 0.01);
            //tasks["FxDeltaGamma"] = pvModel.FxDelta(reportingCcy, currencyProvider, true);
            //tasks["RolledFxDeltaGamma"] = rolledPvModel.FxDelta(reportingCcy, currencyProvider, true);
            //tasks["IrDelta"] = pvModel.AssetIrDelta(reportingCcy);



            //delta
            var baseDeltaGammaCube = tasks["AssetDeltaGamma"];
            cube = cube.Merge(baseDeltaGammaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, mergeTypes: true);

            //vega
            var assetVegaCube = tasks["AssetVega"];
            cube = cube.Merge(assetVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, mergeTypes: true);

            //var fxVegaCube = tasks["FxVega"];
            //cube = cube.Merge(fxVegaCube, new Dictionary<string, object>
            //{
            //     { "Currency", string.Empty },
            //});

            //charm-asset
            //var rolledDeltaGammaCube = AssetDeltaGamma(portfolio, rolledModel);
            var rolledDeltaGammaCube = tasks["RolledDeltaGamma"];
            var rolledParallelGammaCube = tasks["RolledParallelGamma"];

            var baseDeltaCube = baseDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "Delta" } });
            var rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "Delta" } });
            var rolledGammaCube = rolledParallelGammaCube.Filter(new Dictionary<string, object> { { Metric, "ParallelGamma" } });
            var charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            cube = cube.Merge(charmCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
                 { Metric, "Charm"},
            }, mergeTypes: true);
            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { Metric, "AssetDeltaT1" },
            }, mergeTypes: true);
            cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
                {
                    { "Currency", string.Empty },
                }, new Dictionary<string, object>
                {
                    { Metric, "AssetGammaT1" },
                }, mergeTypes: true);

            //charm-fx
            //baseDeltaCube = tasks["FxDeltaGamma"];
            //cube = cube.Merge(baseDeltaCube, new Dictionary<string, object>
            //{
            //    { "PointDate", DateTime.MinValue },
            //    { PointLabel, string.Empty },
            //    { "Currency", string.Empty },
            //});
            //rolledDeltaGammaCube = tasks["RolledFxDeltaGamma"];
            //rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "FxSpotDelta" } });
            //rolledGammaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { Metric, "FxSpotGamma" } });

            //cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            //{
            //     { PointLabel, string.Empty },
            //     { "PointDate", DateTime.MinValue },
            //     { "Currency", string.Empty },
            //}, new Dictionary<string, object>
            //{
            //     { Metric, "FxSpotDeltaT1" },
            //});

            //cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
            //{
            //     { PointLabel, string.Empty },
            //     { "PointDate", DateTime.MinValue },
            //     { "Currency", string.Empty },
            //}, new Dictionary<string, object>
            //{
            //     { Metric, "FxSpotGammaT1" },
            //});

            //charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            //var fId = charmCube.GetColumnIndex(AssetId);
            //foreach (var charmRow in charmCube.GetAllRows())
            //{
            //    var row = new Dictionary<string, object>
            //    {
            //        { TradeId, charmRow.MetaData[tidIx] },
            //        { TradeType, charmRow.MetaData[tTypeIx] },
            //        { AssetId, charmRow.MetaData[fId] },
            //        { PointLabel, string.Empty },
            //        { "PointDate", DateTime.MinValue },
            //        { "Currency", string.Empty },
            //        { Metric, "Charm" }
            //    };
            //    cube.AddRow(row, charmRow.Value);
            //}

            //ir-delta
            //var baseIrDeltacube = tasks["IrDelta"];
            //cube = cube.Merge(baseIrDeltacube, new Dictionary<string, object>
            //{
            //     { "Currency", reportingCcy.Ccy },
            //});

            return cube;
        }

        private static double GetUsdDF(IAssetFxModel model, BasicPriceCurve priceCurve, DateTime fwdDate)
        {
            var colSpec = priceCurve.CollateralSpec;
            var ccy = priceCurve.Currency;
            var disccurve = model.FundingModel.GetCurveByCCyAndSpec(ccy, colSpec);
            return disccurve.GetDf(model.BuildDate, fwdDate);
        }
    }
}
