using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Calibrators;
using Qwack.Utils.Parallel;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk
{
    public static class BenchmarkCurveRisk
    {
        public static ICube BenchmarkRiskWithReStrip(this IPvModel pvModel, FundingInstrumentCollection riskCollection, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency reportingCcy)
        {
            var insByCurve = riskCollection.GroupBy(x => x.SolveCurve);
            var curvesNeeded = insByCurve.Select(x => x.Key).ToArray();
            var newCurves = new List<IrCurve>();

            var newFundingModel = pvModel.VanillaModel.FundingModel.DeepClone(null);
            foreach(var grp in insByCurve)
            {
                var existingCurve = newFundingModel.GetCurve(grp.Key) as IrCurve;
                var pillars = grp.Select(x => x.PillarDate).Distinct().OrderBy(x => x).ToArray();
                var rates = pillars.Select(x=>existingCurve.GetRate(x)).ToArray();

                var newCurve = new IrCurve(pillars, rates, existingCurve.BuildDate, existingCurve.Name, existingCurve.InterpolatorType, existingCurve.Currency, existingCurve.CollateralSpec, existingCurve.RateStorageType)
                {
                    SolveStage = existingCurve.SolveStage, 
                };

                newFundingModel.Curves[grp.Key] = newCurve;
            }

            var sol = new NewtonRaphsonMultiCurveSolverStaged();
            sol.Solve(newFundingModel, riskCollection);
            var newModel = pvModel.VanillaModel.Clone(newFundingModel);

            return newModel.BenchmarkRisk(riskCollection, currencyProvider, calendarProvider, reportingCcy);
        }
        
        public static ICube BenchmarkRisk(this IPvModel pvModel, FundingInstrumentCollection riskCollection, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency reportingCcy)
        {
            var cube = new ResultCube();
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
            };
            cube.Initialize(dataTypes);

            //var lastDate = pvModel.Portfolio.LastSensitivityDate;
            var insByCurve = riskCollection.GroupBy(x => x.SolveCurve);
            var dependencies = riskCollection.FindDependenciesInverse(pvModel.VanillaModel.FundingModel.FxMatrix);
            var lastDateByCurve = insByCurve.ToDictionary(x => x.Key, x => DateTime.MinValue);
            foreach (var ins in pvModel.Portfolio.UnWrapWrappers().Instruments)
            {
                if (ins is IFundingInstrument fins)
                {
                    var cvs = fins.Dependencies(pvModel.VanillaModel.FundingModel.FxMatrix);
                    foreach (var c in cvs)
                    {
                        if (!lastDateByCurve.ContainsKey(c))
                            lastDateByCurve[c] = DateTime.MinValue;

                        lastDateByCurve[c] = lastDateByCurve[c].Max(ins.LastSensitivityDate);
                    }
                }
                else if (ins is IAssetInstrument ains)
                {
                    var cvs = ains.IrCurves(pvModel.VanillaModel);
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
                if (dependencies.ContainsKey(c))
                {
                    foreach (var d in dependencies[c])
                    {
                        lastDateByCurve[c] = lastDateByCurve[c].Max(lastDateByCurve[d]);
                    }
                }
            }


            var insToRisk = new List<IFundingInstrument>();
            foreach (var gp in insByCurve)
            {
                var lastDate = lastDateByCurve[gp.Key];
                var sorted = gp.OrderBy(x => x.LastSensitivityDate).ToList();
                if (sorted.Last().LastSensitivityDate <= lastDate)
                    insToRisk.AddRange(sorted);
                else
                {
                    var lastIns = sorted.First(x => x.LastSensitivityDate > lastDate);
                    var lastIx = sorted.IndexOf(lastIns);
                    lastIx = System.Math.Min(lastIx + 1, sorted.Count);
                    insToRisk.AddRange(sorted.Take(lastIx));
                }
            }

            var parRates = insToRisk.Select(x => x.CalculateParRate(pvModel.VanillaModel.FundingModel)).ToList();
            var newIns = insToRisk.Select((x, ix) => x.SetParRate(parRates[ix]));
            var newFic = new FundingInstrumentCollection(currencyProvider, calendarProvider);
            newFic.AddRange(newIns.OrderBy(x => x.SolveCurve).ThenBy(x => x.PillarDate));

            var fModel = pvModel.VanillaModel.FundingModel.DeepClone(null);
            var s = new NewtonRaphsonMultiCurveSolverStaged() { Tollerance = 0.000000001 };
            s.Solve(fModel, newFic);

            var vModel = pvModel.VanillaModel.Clone(fModel);
            var newPvModel = pvModel.Rebuild(vModel, pvModel.Portfolio);

            //var basePVbyCurrency = new Dictionary<Currency, ICube>();

            var basePV = newPvModel.PV(reportingCcy);

            ParallelUtils.Instance.For(0, newIns.Count(), 1, i =>
            //for (var i = 0; i < newIns.Count(); i++)
            {
                //if (!basePVbyCurrency.TryGetValue(insToRisk[i].Currency, out var basePV))
                //{
                //    basePV = newPvModel.PV(insToRisk[i].Currency);
                //    basePVbyCurrency[insToRisk[i].Currency] = basePV;
                //}

                var tIdIx = basePV.GetColumnIndex(TradeId);
                var tTypeIx = basePV.GetColumnIndex(TradeType);

                var bumpSize = GetBumpSize(insToRisk[i]);

                var bumpedIns = newIns.Select((x, ix) => x.SetParRate(parRates[ix] + (ix == i ? bumpSize : 0.0)));
                var newFicb = new FundingInstrumentCollection(currencyProvider, calendarProvider);
                newFicb.AddRange(bumpedIns);

                var fModelb = fModel.DeepClone(null);

                var sb = new NewtonRaphsonMultiCurveSolverStaged() { Tollerance = 0.0000000001 };
                sb.Solve(fModelb, newFicb);

                var vModelb = pvModel.VanillaModel.Clone(fModelb);
                var newPvModelb = pvModel.Rebuild(vModelb, pvModel.Portfolio);

                //var bumpedPV = newPvModelb.PV(insToRisk[i].Currency);
                var bumpedPV = newPvModelb.PV(reportingCcy);

                var bumpName = insToRisk[i].TradeId;
                var riskDate = insToRisk[i].PillarDate;
                var riskCurve = insToRisk[i].SolveCurve;
                var riskUnits = GetRiskUnits(insToRisk[i]);

                var deltaCube = bumpedPV.QuickDifference(basePV);
           
                foreach (var dRow in deltaCube.GetAllRows())
                {
                    if (dRow.Value == 0.0)
                        continue;

                    var deltaScale = GetScaleFactor(insToRisk[i], parRates[i], parRates[i] + bumpSize, fModel);
                    var fxToCurveCcy = insToRisk[i].Currency == null ? 1.0 : fModel.GetFxRate(fModel.BuildDate, reportingCcy, insToRisk[i].Currency);

                    var row = new Dictionary<string, object>
                            {
                                { TradeId, dRow.MetaData[tIdIx] },
                                { TradeType, dRow.MetaData[tTypeIx] },
                                { "Benchmark", bumpName },
                                { "RiskDate", riskDate },
                                { "Curve", riskCurve },
                                { Metric, "IrBenchmarkDelta" },
                                { "Units", riskUnits },
                                { "BumpSize", bumpSize},
                            };
                    cube.AddRow(row, dRow.Value * deltaScale * fxToCurveCcy);
                }
            }).Wait();

            return cube.Sort(new List<string> { "Curve", "RiskDate", TradeId });
        }

        private static double GetScaleFactor(IFundingInstrument ins, double parFlat, double parBump, IFundingModel model)
        {
            switch (ins)
            {
                case FxForward:
                    return 1.0 / (parBump - parFlat);
                case STIRFuture st:
                    return 1.0 / ((parBump - parFlat) / 0.01 * st.UnitPV01);
                case OISFuture oi:
                    return 1.0 / ((parBump - parFlat) / 0.01 * oi.UnitPV01);
                case ForwardRateAgreement fra:
                    return 1.0 / ((parBump - parFlat) * fra.FlowScheduleFra.Flows.First().YearFraction);
                case ContangoSwap cs:
                    var t360 = (cs.PillarDate - model.BuildDate).TotalDays / 360.0;
                    var spot = model.GetFxRate(model.BuildDate, cs.MetalCCY, cs.CashCCY);
                    var fwdFlat = (1 + parFlat * t360) * spot;
                    var fwdBumped = (1 + parBump * t360) * spot;
                    return 1.0 / (fwdBumped - fwdFlat);
                case IrSwap irs:
                    var pv = irs.SetParRate(parBump).Pv(model, true);
                    return 1.0 / pv * irs.Notional;
                case InflationPerformanceSwap cpi:
                    var pv2a = (cpi.SetParRate(parBump).Pv(model,false) - cpi.SetParRate(parFlat).Pv(model, false));
                    return -1.0 / pv2a * cpi.Notional;
                case FloatingRateLoanDepo fld:
                    var pvF = fld.SetParRate(parBump).Pv(model, true);
                    return 1.0 / pvF * fld.Notional;
                case FixedRateLoanDeposit fxd:
                    var pvFxd = fxd.SetParRate(parBump).Pv(model, true);
                    return 1.0 / pvFxd * fxd.Notional;
                default:
                    return 1.0;
            }
        }
        private static double GetBumpSize(IFundingInstrument ins) => ins switch
        {
            STIRFuture or OISFuture or InflationPerformanceSwap => 0.01,
            //STIRFuture or OISFuture => 0.01,
            _ => 0.0001,
        };
        private static string GetRiskUnits(IFundingInstrument ins) => ins switch
        {
            STIRFuture or OISFuture => "Contracts",
            ForwardRateAgreement or FxForward or IrSwap or FloatingRateLoanDepo or FixedRateLoanDeposit or InflationPerformanceSwap => "Nominal",
            ContangoSwap => "Oz",
            _ => "PnL",
        };

    }
}
