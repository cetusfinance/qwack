using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Calibrators;
using Qwack.Models.Models;
using Qwack.Utils.Parallel;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk
{
    public static class AssetBenchmarkCurveRisk
    {
        public static ICube Produce(IAssetFxModel pvModel, List<AssetCurveBenchmarkSpec> curveSpecs, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency baseCurrency, bool restrip = true)
        {
            var o = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { TradeId, typeof(string) },
                { TradeType,  typeof(string) },
                { "Curve", typeof(string) },
                { "RiskDate", typeof(DateTime) },
                { "Benchmark", typeof(string) },
                { Metric, typeof(string) },
                { "Units", typeof(string) },
                { "BumpSize", typeof(double) },
                { "Portfolio", typeof(string) }
            };

            o.Initialize(dataTypes);

            var specsByName = curveSpecs.ToDictionary(x => x.CurveName, x => x);
            //reprice all instruments to base
            var basePrices = curveSpecs.SelectMany(x => x.Instruments).ToDictionary(x => x.TradeId, x => x.ParRate(pvModel));
            var insToRisk = curveSpecs.SelectMany(x => x.Instruments).Select(x => x.SetStrike(basePrices[x.TradeId])).ToList();
            var curveToInsMap = curveSpecs.SelectMany(x => x.Instruments).ToDictionary(x => x.TradeId, x => curveSpecs.FirstOrDefault(y => y.Instruments.Any(z => z.TradeId == x.TradeId)));
            var baseCurves = restrip ? PriceCurves(pvModel, curveSpecs, basePrices, currencyProvider, calendarProvider, baseCurrency) :
                PriceCurvesWithoutRestrip(pvModel, curveSpecs, basePrices, currencyProvider, calendarProvider, baseCurrency);
            var reBaseModel = pvModel.Clone();
            reBaseModel.AddPriceCurves(baseCurves);
            var basePvCube = reBaseModel.PV(baseCurrency);

            var lastDateByCurve = new Dictionary<string, DateTime>();
            foreach (var ins in pvModel.Portfolio.UnWrapWrappers().Instruments)
            {
                if (ins is IAssetInstrument ains)
                {
                    var cvs = ains.AssetIds;
                    foreach (var c in cvs)
                    {
                        if (!lastDateByCurve.ContainsKey(c))
                            lastDateByCurve[c] = DateTime.MinValue;

                        lastDateByCurve[c] = lastDateByCurve[c].Max(ins.LastSensitivityDate);
                    }
                }
            }

            foreach (var c in lastDateByCurve.Keys.ToArray())
            {
                var spec = specsByName[c];
                if (spec.DependsOnCurves != null && spec.DependsOnCurves.Contains(c))
                {
                    foreach (var d in spec.DependsOnCurves)
                    {
                        lastDateByCurve[c] = lastDateByCurve[c].Max(lastDateByCurve[d]);
                    }
                }
            }

            ParallelUtils.Instance.For(0, insToRisk.Count(), 1, i =>
            //for (var i = 0; i < newIns.Count(); i++)
            {


                var tIdIx = basePvCube.GetColumnIndex(TradeId);
                var tTypeIx = basePvCube.GetColumnIndex(TradeType);

                var bumpSize = GetBumpSize(insToRisk[i]);

                var bumpedPrices = basePrices.ToDictionary(x => x.Key, x => x.Value + (x.Key == insToRisk[i].TradeId ? bumpSize : 0.0));
                var bumpedCurves = restrip ? PriceCurves(pvModel, curveSpecs, bumpedPrices, currencyProvider, calendarProvider, baseCurrency) 
             :PriceCurvesWithoutRestrip(pvModel, curveSpecs, bumpedPrices, currencyProvider, calendarProvider, baseCurrency);

                var bumpedModel = pvModel.Clone();
                bumpedModel.AddPriceCurves(bumpedCurves);
                var bumpedPvCuve = bumpedModel.PV(baseCurrency);

                //var bumpedPV = newPvModelb.PV(insToRisk[i].Currency);

                var bumpName = insToRisk[i].TradeId;
                var riskDate = SuggestPillarDate(insToRisk[i]);
                var riskCurve = curveToInsMap[insToRisk[i].TradeId].CurveName;
                var riskUnits = GetRiskUnits(insToRisk[i]);

                var deltaCube = bumpedPvCuve.QuickDifference(basePvCube);

                foreach (var dRow in deltaCube.GetAllRows())
                {
                    if (dRow.Value == 0.0)
                        continue;

                    var deltaScale = GetScaleFactor(insToRisk[i], basePrices[insToRisk[i].TradeId], basePrices[insToRisk[i].TradeId] + bumpSize, pvModel);
                    var fxToCurveCcy = insToRisk[i].Currency == null ? 1.0 : pvModel.FundingModel.GetFxRate(pvModel.BuildDate, baseCurrency, insToRisk[i].Currency);

                    var row = new Dictionary<string, object>
                            {
                                { TradeId, dRow.MetaData[tIdIx] },
                                { TradeType, dRow.MetaData[tTypeIx] },
                                { "Benchmark", bumpName },
                                { "RiskDate", riskDate },
                                { "Curve", riskCurve },
                                { Metric, "AssetBenchmarkDelta" },
                                { "Units", riskUnits },
                                { "BumpSize", bumpSize},
                            };
                    o.AddRow(row, dRow.Value * deltaScale * fxToCurveCcy);
                }
            }).Wait();



            return o;
        }
        public static DateTime SuggestPillarDate(IAssetInstrument ins) => ins switch
        {
            Future fut => fut.ExpiryDate,
            AsianBasisSwap bs => bs.LastSensitivityDate,
            AsianSwap aswp => aswp.AverageEndDate,
            Forward fwd => fwd.ExpiryDate,
            _ => default,
        };

        private static Dictionary<string, IPriceCurve> PriceCurves(IAssetFxModel baseModel, List<AssetCurveBenchmarkSpec> curveSpecs, Dictionary<string, double> insPrices, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency baseCurrency)
        {
            List<AssetCurveBenchmarkSpec> specs = new();
            Dictionary<string, AssetCurveBenchmarkSpec> specsNew = new();
            Dictionary<string, IPriceCurve> curves = new();
            HashSet<string> curvesStillToSolve = new();
            HashSet<string> curvesSolved = new();

            var solver = new NewtonRaphsonAssetCurveSolver();

            foreach (var spec in curveSpecs)
            {
                var disco = new ConstantRateIrCurve(0, baseModel.BuildDate, "ZERO", baseCurrency);

                var specNew = new AssetCurveBenchmarkSpec
                {
                    CurveName = spec.CurveName,
                    CurveType = spec.CurveType,
                    Instruments = spec.Instruments.Select(x => x.Clone().SetStrike(insPrices[x.TradeId])).OrderBy(SuggestPillarDate).ToList(),
                    DependsOnCurves = spec.DependsOnCurves
                };

                specsNew[spec.CurveName] = specNew;

                var curve = new BasicPriceCurve(baseModel.BuildDate, specNew.Instruments.Select(SuggestPillarDate).ToArray(), specNew.Instruments.Select(x => 100.0).ToArray(), spec.CurveType, currencyProvider)
                {
                    Name = spec.CurveName,
                };
               
                if (spec.DependsOnCurves == null || spec.DependsOnCurves.Length == 0)
                {
                    curve = (BasicPriceCurve)solver.Solve(specNew.Instruments, curve, null, disco, baseModel.BuildDate, currencyProvider, calendarProvider);
                    curvesSolved.Add(spec.CurveName);
                }
                else
                {
                    curvesStillToSolve.Add(spec.CurveName);
                }

                curves[spec.CurveName] = curve;
            }

            var breakout = 0;
            while (breakout < 10 && curvesStillToSolve.Any())
            {
                var curvesForNextPass = new HashSet<string>();
                foreach (var curveName in curvesStillToSolve)
                {
                    var specNew = specsNew[curveName];
                    if (specNew.DependsOnCurves.All(x => curvesSolved.Contains(x)))
                    {
                        //solve
                        var disco = new ConstantRateIrCurve(0, baseModel.BuildDate, "ZERO", baseCurrency);
                        var curve = curves[specNew.CurveName];
                        var dependencies = specNew.DependsOnCurves.Select(x => (IPriceCurve)curves[x]).ToList();
                        curves[specNew.CurveName] = solver.Solve(specNew.Instruments, curve, dependencies, disco, baseModel.BuildDate, currencyProvider, calendarProvider);
                        curvesSolved.Add(curveName);
                    }
                    else
                        curvesForNextPass.Add(curveName);
                }
                curvesStillToSolve = curvesForNextPass;
                breakout++;
            }


            return curves;
        }

        private static Dictionary<string, IPriceCurve> PriceCurvesWithoutRestrip(IAssetFxModel baseModel, List<AssetCurveBenchmarkSpec> curveSpecs, Dictionary<string, double> insPrices, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency baseCurrency)
        {
            List<AssetCurveBenchmarkSpec> specs = new();
            Dictionary<string, AssetCurveBenchmarkSpec> specsNew = new();
            Dictionary<string, IPriceCurve> curves = new();
            HashSet<string> curvesStillToSolve = new();
            HashSet<string> curvesSolved = new();

            var solver = new NewtonRaphsonAssetCurveSolver();

            foreach (var spec in curveSpecs)
            {
                var disco = new ConstantRateIrCurve(0, baseModel.BuildDate, "ZERO", baseCurrency);

                var specNew = new AssetCurveBenchmarkSpec
                {
                    CurveName = spec.CurveName,
                    CurveType = spec.CurveType,
                    Instruments = spec.Instruments.Select(x => x.Clone().SetStrike(insPrices[x.TradeId])).OrderBy(SuggestPillarDate).ToList(),
                    DependsOnCurves = spec.DependsOnCurves
                };

                specsNew[spec.CurveName] = specNew;

                var curve = (baseModel.GetPriceCurve(spec.CurveName) as BasicPriceCurve).Clone();
                if (spec.DependsOnCurves == null || spec.DependsOnCurves.Length == 0)
                {
                    curve = (BasicPriceCurve)solver.Solve(specNew.Instruments, curve, null, disco, baseModel.BuildDate, currencyProvider, calendarProvider);
                    curvesSolved.Add(spec.CurveName);
                }
                else
                {
                    curvesStillToSolve.Add(spec.CurveName);
                }

                curves[spec.CurveName] = curve;
            }

            var breakout = 0;
            while (breakout < 10 && curvesStillToSolve.Any())
            {
                var curvesForNextPass = new HashSet<string>();
                foreach (var curveName in curvesStillToSolve)
                {
                    var specNew = specsNew[curveName];
                    if (specNew.DependsOnCurves.All(x => curvesSolved.Contains(x)))
                    {
                        //solve
                        var disco = new ConstantRateIrCurve(0, baseModel.BuildDate, "ZERO", baseCurrency);
                        var curve = curves[specNew.CurveName];
                        var dependencies = specNew.DependsOnCurves.Select(x => (IPriceCurve)curves[x]).ToList();
                        curves[specNew.CurveName] = solver.Solve(specNew.Instruments, curve, dependencies, disco, baseModel.BuildDate, currencyProvider, calendarProvider);
                        curvesSolved.Add(curveName);
                    }
                    else
                        curvesForNextPass.Add(curveName);
                }
                curvesStillToSolve = curvesForNextPass;
                breakout++;
            }


            return curves;
        }


        private static double GetBumpSize(IAssetInstrument ins) => ins switch
        {
            _ => 0.0001,
        };
        private static string GetRiskUnits(IAssetInstrument ins) => ins switch
        {
            Future f => "Lots",
            _ => "Units",
        };

        private static double GetScaleFactor(IAssetInstrument ins, double parFlat, double parBump, IAssetFxModel model)
        {
            switch (ins)
            {
                case Future f:
                    return 1.0 / ((parBump - parFlat)) / f.LotSize;
                case AsianBasisSwap ck:
                    return 1.0 / ((parBump - parFlat)) / ck.PaySwaplets.First().Notional;
                default:
                    return 1.0;
            }
        }
    }
}
