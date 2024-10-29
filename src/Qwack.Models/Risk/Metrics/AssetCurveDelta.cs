using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Utils.Parallel;
using static Qwack.Core.Basic.Consts.Cubes;
using static System.Math;

namespace Qwack.Models.Risk.Metrics
{
    public class AssetCurveDelta(IPvModel pvModel, bool computeGamma = false, DateTime[] pointsToBump = null, bool isSparseLMEMode = false, ICalendarProvider calendars = null, double bumpSize = 0.01) : IRiskMetric
    {
        private readonly Dictionary<string, double> _bumpsForCurve = [];
        private readonly Dictionary<string, Currency> _ccysForCurve = [];

        public ICube ComputeSync(bool parallelize = false)
        {
            var models = GenerateScenarios();
            var results = new Dictionary<string, ICube>();
            if (parallelize) 
            {
                ParallelUtils.Instance.Foreach(models.ToList(), m =>
                {
                    var curveName = m.Key.Split('¬')[0];
                    var result = m.Value.PV(_ccysForCurve[curveName]);
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
                    var result = m.Value.PV(_ccysForCurve[curveName]);
                    results[m.Key] = result;
                }
            }
            var finalResult = GenerateCubeFromResults(results, models);
            return finalResult;
        }

        public ICube GenerateCubeFromResults(Dictionary<string, ICube> results, Dictionary<string, IPvModel> models)
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

            var curveNames = results.Keys.Where(x => x.EndsWith("¬BASE")).Select(x => x.Split('¬')[0]).Distinct().ToList();

            foreach (var curveName in curveNames)
            {
                var bumpForCurve = _bumpsForCurve[curveName];
                var pvCube = results[$"{curveName}¬BASE"];
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex(TradeId);
                var tTypeIx = pvCube.GetColumnIndex(TradeType);
                var pfIx = pvCube.GetColumnIndex(Consts.Cubes.Portfolio);

                var resultsForCurve = results.Where(x => x.Key.StartsWith(curveName)).ToList();
                var primaryKeys = resultsForCurve.Where(x => x.Key.StartsWith(curveName) && x.Key != $"{curveName}¬BASE" && !x.Key.Contains("¬DOWN¬")).ToList();


                foreach (var kv in primaryKeys)
                {
                    var bumpedPVCube = kv.Value;
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    var model = models[kv.Key];
                    var bCurve = model.VanillaModel.GetPriceCurve(curveName);

                    ResultCubeRow[] bumpedRowsDown = null;
                    if (computeGamma)
                    {
                        var bumpedPVCubeDown = results[kv.Key.Replace("¬", "¬DOWN¬")];
                        bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                        if (bumpedRowsDown.Length != pvRows.Length)
                            throw new Exception("Dimensions do not match");
                    }

                    var pointLabel = kv.Key.Split('¬')[1];


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
                                if (bCurve.UnderlyingsAreForwards) //de-discount delta
                                    delta /= Utils.GetUsdDF(model.VanillaModel, (BasicPriceCurve)bCurve, bCurve.PillarDatesForLabel(pointLabel));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, bumpedRows[i].MetaData[tidIx] },
                                    { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", bCurve.PillarDatesForLabel(pointLabel) },
                                    { PointLabel, pointLabel },
                                    { Metric, "Delta" },
                                    { "CurveType", bCurve is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.GetPriceForDate(bCurve.PillarDatesForLabel(pointLabel)) },
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
                                    { "PointDate", bCurve.PillarDatesForLabel(pointLabel) },
                                    { PointLabel, pointLabel },
                                    { Metric, "Gamma" },
                                    { "CurveType", bCurve is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.GetPriceForDate(bCurve.PillarDatesForLabel(pointLabel)) },
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
                                if (bCurve.UnderlyingsAreForwards) //de-discount delta
                                    delta /= Utils.GetUsdDF(model.VanillaModel, (BasicPriceCurve)bCurve, bCurve.PillarDatesForLabel(pointLabel));

                                var row = new Dictionary<string, object>
                                {
                                    { TradeId, bumpedRows[i].MetaData[tidIx] },
                                    { TradeType, bumpedRows[i].MetaData[tTypeIx] },
                                    { AssetId, curveName },
                                    { "PointDate", bCurve.PillarDatesForLabel(pointLabel) },
                                    { PointLabel, pointLabel},
                                    { Metric, "Delta" },
                                    { "CurveType", bCurve is BasisPriceCurve ? "Basis" : "Outright" },
                                    { "RefPrice", bCurve.GetPriceForDate(bCurve.PillarDatesForLabel(pointLabel)) },
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
                }
            }
            return cube;
        }
        public Dictionary<string, IPvModel> GenerateScenarios()
        {
            var  o = new Dictionary<string, IPvModel>();

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
                    (ia.AssetIds.Contains(curveObj.AssetId) || ia.AssetIds.Any(aid => allLinkedCurves.Contains(aid))))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate;

                var baseModel = pvModel.Rebuild(model, subPortfolio);

                o[$"{curveName}¬BASE"] = baseModel;
                _bumpsForCurve[curveName] = bumpForCurve;
                _ccysForCurve[curveName] = curveObj.Currency;

                Dictionary<string, IPriceCurve> bumpedCurves;
                Dictionary<string, IPriceCurve> bumpedDownCurves;
                if (isSparseLMEMode && curveObj is BasicPriceCurve bpc && bpc.CurveType == Transport.BasicTypes.PriceCurveType.LME)
                {
                    lastDateInBook = Utils.NextThirdWeds(lastDateInBook);
                    var sparseDates = curveObj.PillarDates.Where(x => x <= lastDateInBook && DateExtensions.IsSparseLMEDate(x, curveObj.BuildDate, calendars)).ToArray();
                    bumpedCurves = curveObj.GetDeltaScenarios(bumpForCurve, lastDateInBook, sparseDates);
                    bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpForCurve, lastDateInBook, sparseDates) : null;
                }
                else
                {
                    bumpedCurves = curveObj.GetDeltaScenarios(bumpForCurve, lastDateInBook, pointsToBump);
                    bumpedDownCurves = computeGamma ? curveObj.GetDeltaScenarios(-bumpForCurve, lastDateInBook, pointsToBump) : null;
                }

                foreach (var bCurve in bumpedCurves)
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
                            var recalCurve = ((BasisPriceCurve)newVanillaModel.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                            newVanillaModel.AddPriceCurve(depCurveName, recalCurve);
                            newBaseCurves.Add(depCurveName);
                        }

                        dependentCurves = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                    }
                    var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                    o[$"{curveName}¬{bCurve.Key}"] = baseModel;
                }

                if (bumpedDownCurves != null)
                {
                    foreach (var bCurve in bumpedDownCurves)
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
                                var recalCurve = ((BasisPriceCurve)newVanillaModel.GetPriceCurve(depCurveName)).ReCalibrate(baseCurve);
                                newVanillaModel.AddPriceCurve(depCurveName, recalCurve);
                                newBaseCurves.Add(depCurveName);
                            }

                            dependentCurves = newBaseCurves.SelectMany(x => model.GetDependentCurves(x)).Distinct().ToList();
                        }
                        var newPvModel = pvModel.Rebuild(newVanillaModel, subPortfolio);

                        o[$"{curveName}¬DOWN¬{bCurve.Key}"] = baseModel;
                    }
                }
            }

            return o;
        }
    }
}
