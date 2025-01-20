using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Utils.Parallel;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk.Metrics
{
    public class AssetVega(IPvModel pvModel, Currency reportingCcy, double bumpSize = -0.01) : IRiskMetric
    {
        private readonly Dictionary<string, double> _strikesByTradeId = [];

        public ICube ComputeSync(bool parallelize = false)
        {
            var models = GenerateScenarios();
            var results = new Dictionary<string, ICube>();
            if (parallelize) 
            {
                ParallelUtils.Instance.Foreach(models.ToList(), m =>
                {
                    var curveName = m.Key.Split('¬')[0];
                    var result = m.Value.PV(reportingCcy);
                    lock (results)
                    {
                        results[m.Key] = result;
                    }
                }).Wait();
            }
            else
            {
                foreach (var m in models)
                {
                    var curveName = m.Key.Split('¬')[0];
                    var result = m.Value.PV(reportingCcy);
                    results[m.Key] = result;
                }
            }
            var finalResult = GenerateCubeFromResults(results, models);
            return finalResult;
        }

        public void Dispose() {}

        public ICube GenerateCubeFromResults(Dictionary<string, ICube> results, Dictionary<string, IPvModel> models)
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

            var surfaceNames = results.Keys.Where(x => x.EndsWith("¬BASE")).Select(x => x.Split('¬')[0]).Distinct().ToList();

            foreach (var surfaceName in surfaceNames)
            {
                var bumpForCurve = bumpSize;
                var pvCube = results[$"{surfaceName}¬BASE"];
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var resultsForCurve = results.Where(x => x.Key.StartsWith(surfaceName)).ToList();
                var primaryKeys = resultsForCurve.Where(x => x.Key.StartsWith(surfaceName) && x.Key != $"{surfaceName}¬BASE" && !x.Key.Contains("¬DOWN¬")).ToList();


                foreach (var kv in primaryKeys)
                {
                    var bumpedPVCube = kv.Value;
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    var model = models[kv.Key];
                    var bCurve = model.VanillaModel.GetPriceCurve(surfaceName);
                    var bSurf = model.VanillaModel.GetVolSurface(surfaceName);
                    var pointLabel = kv.Key.Split('¬')[1];


                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpForCurve * 0.01;

                        if (vega != 0.0)
                        {
                            var trdId = bumpedRows[i].MetaData[tidIx] as string;
                            var pillarDate = bSurf.PillarDatesForLabel(pointLabel);
                            var fwdPrice = bCurve?.GetPriceForDate(pillarDate) ?? 100;
                            var cleanVol = model.VanillaModel.GetVolForDeltaStrikeAndDate(bCurve.AssetId, pillarDate, 0.5);
                            var row = new Dictionary<string, object>
                            {
                                { TradeId, trdId },
                                { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                { AssetId, surfaceName },
                                { "PointDate", pillarDate },
                                { PointLabel, pointLabel },
                                { Metric, "Vega" },
                                { "Strike", _strikesByTradeId[trdId] },
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
                }
            }
            return cube.Sort(new List<string> { AssetId, "PointDate", TradeType }); 
        }
        public Dictionary<string, IPvModel> GenerateScenarios()
        {
            var  o = new Dictionary<string, IPvModel>();
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

                using var baseModel = pvModel.Rebuild(model, subPortfolio);
                o[$"{surfaceName}¬BASE"] = baseModel;


                var strikesByTradeId = subPortfolio.Instruments.ToDictionary(t => t.TradeId, t => t.GetStrike());
                foreach(var kv in strikesByTradeId)
                {
                    _strikesByTradeId[kv.Key] = kv.Value;
                }

                var lastDateInBook = subPortfolio.LastSensitivityDate;
                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                foreach(var bCurve in bumpedSurfaces.ToList())
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    using var bumpedPvModel = baseModel.Rebuild(newVanillaModel, subPortfolio);
                    o[$"{surfaceName}¬{bCurve.Key}"] = bumpedPvModel;
                }
            }

            return o;
        }
    }
}
