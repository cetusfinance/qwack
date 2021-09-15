using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Options.VolSurfaces;
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

        private static double GetStrike(this IInstrument ins) => ins switch
        {
            null => 0.0,
            EuropeanOption euo => euo.Strike,
            FuturesOption fuo => fuo.Strike,
            _ => 0.0,
        };
        public static ICube AssetVega(this IPvModel pvModel, Currency reportingCcy, bool parallelize = true)
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
                { "Strike", typeof(double) }
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

                var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedSurfaces.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
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
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, trdId },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Vega" },
                                { "Strike", strikesByTradeId[trdId] }
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

            return cube.Sort(new List<string> {AssetId,"PointDate",TradeType});
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

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                if (!(model.GetVolSurface(surfaceName) is RiskyFlySurface volObj))
                    continue;

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments.Where(x => (x is IHasVega) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                var bumpedSurfacesSega = volObj.GetSegaScenarios(bumpSize, lastDateInBook);
                var bumpedSurfacesRega = volObj.GetRegaScenarios(bumpSize, lastDateInBook);

                var t1 = ParallelUtils.Instance.Foreach(bumpedSurfacesSega.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
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
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
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

            var subPortfolio = new Portfolio()
            {
                Instruments = model.Portfolio.Instruments.Where(x => x is IHasVega || (x is CashWrapper cw && cw.UnderlyingInstrument is IHasVega)).ToList()
            };

            if (subPortfolio.Instruments.Count == 0)
                return cube;

            var lastDateInBook = subPortfolio.LastSensitivityDate;
            var basePvModel = pvModel.Rebuild(model, subPortfolio);
            var pvCube = basePvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            foreach (var surface in model.FundingModel.VolSurfaces)
            {
                var volObj = surface.Value;
                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedSurfaces.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.FundingModel.VolSurfaces[surface.Key] = bCurve.Value;
                    var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
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
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, bumpedRows[i].MetaData[tidIx] },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surface.Key },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { PointLabel, bCurve.Key },
                                { Metric, "Vega" }
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

        public static ICube AssetDeltaSingleCurve(this IPvModel pvModel, string assetId, bool computeGamma = false)
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

            var curveName = model.Curves.Where(x => x.AssetId == assetId).First().Name;

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

            var pvCube = subPortfolio.PV(model, curveObj.Currency);
            var pvRows = pvCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);
            var bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook) : null;

            ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
            {
                var newVanillaModel = model.Clone();
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
                var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                if (computeGamma)
                {
                    var newVanillaModelDown = model.Clone();
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
                    var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                    var bumpedPVCubeDown = newPvModelDown.PV(curveObj.Currency);
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
            }).Wait();

            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }



        public static ICube AssetDelta(this IPvModel pvModel, bool computeGamma = false, bool parallelize=false)
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
                { "RefPrice", typeof(double) },
            };
            var metaKeys = pvModel.Portfolio.Instruments.Where(x => x.TradeId != null).SelectMany(x => x.MetaData.Keys).Distinct().ToArray();
            foreach(var key in metaKeys)
            {
                dataTypes[key] = typeof(string);
            }
            var insDict = pvModel.Portfolio.Instruments.Where(x=>x.TradeId!=null).ToDictionary(x => x.TradeId, x => x);

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

                var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);
                var bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook) : null;

                ParallelUtils.Instance.Foreach(bumpedCurves.ToList(), bCurve =>
                {
                    var newVanillaModel = model.Clone();
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
                    var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                    var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    ResultCubeRow[] bumpedRowsDown = null;
                    if(computeGamma)
                    {
                        var newVanillaModelDown = model.Clone();
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
                        var newPvModelDown = pvModel.Rebuild(newVanillaModelDown, subPortfolio);

                        var bumpedPVCubeDown = newPvModelDown.PV(curveObj.Currency);
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
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
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
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
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
                                { "CurveType", bCurve.Value is BasisPriceCurve ? "Basis" : "Outright" },
                                { "RefPrice", bCurve.Value.GetPriceForDate(bCurve.Value.PillarDatesForLabel(bCurve.Key)) },
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
                },!(parallelize)).Wait();
            }
            return cube.Sort(new List<string> { AssetId, "CurveType", "PointDate", TradeId });
        }

        public static ICube AssetCashDelta(this IPvModel pvModel, Currency reportingCurrency=null)
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
                { "Currency", typeof(string) }
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
                            if(reportingCurrency!=null && bCurve.Value.Currency!=reportingCurrency)
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


        public static ICube AssetParallelDelta(this IPvModel pvModel, ICurrencyProvider currencyProvider)
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

                var baseModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = baseModel.PV(curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);

                IPriceCurve bumpedCurve;

                switch (curveObj)
                {
                    case ConstantPriceCurve con:
                        bumpedCurve = new ConstantPriceCurve(con.Price * 1.01, con.BuildDate, currencyProvider);
                        break;
                    case ContangoPriceCurve cpc:
                        bumpedCurve = new ContangoPriceCurve(cpc.BuildDate, cpc.Spot * 1.01, cpc.SpotDate, cpc.PillarDates, cpc.Contangos, currencyProvider, cpc.Basis, cpc.PillarLabels);
                        break;
                    case BasicPriceCurve pc:
                        bumpedCurve = new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select(p => p * 1.01).ToArray(), pc.CurveType, currencyProvider, pc.PillarLabels);
                        break;
                    default:
                        throw new Exception("Unable to handle curve type for flat shift");
                }

                var newVanillaModel = model.Clone();
                newVanillaModel.AddPriceCurve(curveName, bumpedCurve);

                var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                var bumpedPVCube = newPvModel.PV(curveObj.Currency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / 0.01;

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

        public static ICube FxDelta(this IPvModel pvModel, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false, bool reportInverseDelta=false)
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

            var domCcy = model.FundingModel.FxMatrix.BaseCurrency;

            if (homeCcy != null && homeCcy != domCcy)//remap onto new base currency
            {
                domCcy = homeCcy;
                mf = FundingModel.RemapBaseCurrency(mf, homeCcy, currencyProvider);
            }

            var m = model.Clone(mf);

            foreach (var currency in m.FundingModel.FxMatrix.SpotRates.Keys)
            {
                var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
                var pvCube = newPvModel.PV(m.FundingModel.FxMatrix.BaseCurrency);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex("Portfolio");

                var fxPair = $"{domCcy}/{currency}";
                //var fxPair = $"{currency}/{domCcy}";

                var newModel = m.Clone();
                var bumpedSpot = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 + bumpSize);
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var inverseSpotBump = reportInverseDelta ? 1 / bumpedSpot - 1 / m.FundingModel.FxMatrix.SpotRates[currency] : bumpedSpot - m.FundingModel.FxMatrix.SpotRates[currency];
                var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
                var bumpedPVCube = bumpedPvModel.PV(m.FundingModel.FxMatrix.BaseCurrency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                var inverseSpotBumpDown = 0.0;

                var dfToSpotDate = m.FundingModel.GetDf(m.FundingModel.FxMatrix.BaseCurrency, m.BuildDate, m.FundingModel.FxMatrix.GetFxPair(fxPair).SpotDate(m.BuildDate));

                if (computeGamma)
                {
                    var bumpedSpotDown = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 - bumpSize);
                    newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpotDown;
                    inverseSpotBumpDown = reportInverseDelta ? 1 / bumpedSpotDown - 1 / m.FundingModel.FxMatrix.SpotRates[currency] : bumpedSpotDown - m.FundingModel.FxMatrix.SpotRates[currency];

                    var bumpedPvModelDown = pvModel.Rebuild(newModel, pvModel.Portfolio);

                    var bumpedPVCubeDown = bumpedPvModelDown.PV(m.FundingModel.FxMatrix.BaseCurrency);
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / inverseSpotBump / dfToSpotDate;

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
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / inverseSpotBumpDown / dfToSpotDate;
                        var gamma = (delta - deltaDown) / (inverseSpotBump - inverseSpotBumpDown) * 2.0;
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
                var homeBasePair = mf.FxMatrix.GetFxPair(homeCcy, currency);
                var homeToBase = mf.GetFxRate(homeBasePair.SpotDate(model.BuildDate), homeCcy, currency);

                var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
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
                var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
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
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / spotBump / dfToSpotDate * homeToBase;

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
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / spotBumpDown / dfToSpotDate * homeToBase;
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

                var newPvModel = pvModel.Rebuild(m, pvModel.Portfolio);
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
                var bumpedPvModel = pvModel.Rebuild(newModel, pvModel.Portfolio);
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


        public static ICube AssetIrDelta(this IPvModel pvModel, Currency reportingCcy = null, double bumpSize=0.0001)
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
                var curveObj = curve.Value;

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) && (ia.IrCurves(model).Contains(curve.Key) || (reportingCcy != null && reportingCcy != ia.Currency)))
                    .ToList()
                };
                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var subModel = pvModel.Rebuild(pvModel.VanillaModel, subPortfolio);
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
                    var newPvModel = pvModel.Rebuild(newModel, subPortfolio);
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
                }).Wait();
            }

            return cube.Sort();
        }

        public static ICube AssetThetaCharm(this IPvModel pvModel, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider, bool computeCharm = false, List<FxPair> FxPairsToRisk = null)
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

            var pvCube = pvModel.PV(reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = pvModel.Portfolio.FlowsT0(model, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex(TradeId);
            var tTypeIx = pvCube.GetColumnIndex(TradeType);

            var rolledVanillaModel = model.RollModel(fwdValDate, currencyProvider);
            var rolledPvModel = pvModel.Rebuild(rolledVanillaModel, pvModel.Portfolio);

            var pvCubeFwd = rolledPvModel.PV(reportingCcy);
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
                        { AssetId, string.Empty },
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
                    }
                    cube.AddRow(row, cash);
                }
            }

            //charm-asset
            if (computeCharm)
            {
                var baseDeltaCube = pvModel.AssetDelta();
                var rolledDeltaCube = rolledPvModel.AssetDelta();
                var charmCube = rolledDeltaCube.Difference(baseDeltaCube);
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

            foreach(var t in thetasByTrade)
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
            var rolledPvModel = pvModel.Rebuild(rolledVanillaModel, pvModel.Portfolio);

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

        private static double GetUsdDF(IAssetFxModel model, BasicPriceCurve priceCurve, DateTime fwdDate)
        {
            var colSpec = priceCurve.CollateralSpec;
            var ccy = priceCurve.Currency;
            var disccurve = model.FundingModel.GetCurveByCCyAndSpec(ccy, colSpec);
            return disccurve.GetDf(model.BuildDate, fwdDate);
        }
    }
}
