using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Models.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Utils.Parallel;
using Qwack.Dates;
using System.ComponentModel.DataAnnotations;

namespace Qwack.Models.Risk
{
    public static class BenchmarkCurveRisk
    {
        public static ICube BenchmarkRisk(this IPvModel pvModel, FundingInstrumentCollection riskCollection, ICurrencyProvider currencyProvider, Currency reportingCcy)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType",  typeof(string) },
                { "Curve", typeof(string) },
                { "RiskDate", typeof(DateTime) },
                { "Benchmark", typeof(string) },
                { "Metric", typeof(string) },
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
                if(ins is IFundingInstrument fins)
                {
                    var cvs = fins.Dependencies(pvModel.VanillaModel.FundingModel.FxMatrix);
                    foreach(var c in cvs)
                    {
                        if (!lastDateByCurve.ContainsKey(c))
                            lastDateByCurve[c] = DateTime.MinValue;

                        lastDateByCurve[c] = lastDateByCurve[c].Max(ins.LastSensitivityDate);
                    }
                }
                else if(ins is IAssetInstrument ains)
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

            foreach(var c in lastDateByCurve.Keys.ToArray())
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
            foreach(var gp in insByCurve)
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
            var newFic = new FundingInstrumentCollection(currencyProvider);
            newFic.AddRange(newIns.OrderBy(x=>x.SolveCurve).ThenBy(x=>x.PillarDate));

            var fModel = pvModel.VanillaModel.FundingModel.DeepClone(null);
            var s = new NewtonRaphsonMultiCurveSolverStaged();
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

                var tIdIx = basePV.GetColumnIndex("TradeId");
                var tTypeIx = basePV.GetColumnIndex("TradeType");

                var bumpSize = GetBumpSize(insToRisk[i]);

                var bumpedIns = newIns.Select((x, ix) => x.SetParRate(parRates[ix] + (ix == i ? bumpSize : 0.0)));
                var newFicb = new FundingInstrumentCollection(currencyProvider);
                newFicb.AddRange(bumpedIns);

                var fModelb = fModel.DeepClone(null);

                var sb = new NewtonRaphsonMultiCurveSolverStaged();
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
                var deltaScale = GetScaleFactor(insToRisk[i], parRates[i], parRates[i] + bumpSize, fModel);
                var fxToCurveCcy = fModel.GetFxRate(fModel.BuildDate, reportingCcy, insToRisk[i].Currency);

                foreach (var dRow in deltaCube.GetAllRows())
                {
                    if (dRow.Value == 0.0)
                        continue;

                    var row = new Dictionary<string, object>
                            {
                                { "TradeId", dRow.MetaData[tIdIx] },
                                { "TradeType", dRow.MetaData[tTypeIx] },
                                { "Benchmark", bumpName },
                                { "RiskDate", riskDate },
                                { "Curve", riskCurve },
                                { "Metric", "IrBenchmarkDelta" },
                                { "Units", riskUnits },
                                { "BumpSize", bumpSize},
                            };
                    cube.AddRow(row, dRow.Value * deltaScale * fxToCurveCcy);
                }
            }).Wait();

            return cube.Sort(new List<string> {"Curve","RiskDate","TradeId"});
        }

        private static double GetScaleFactor(IFundingInstrument ins, double parFlat, double parBump, IFundingModel model)
        {
            switch (ins)
            {
                case FxForward fxf:
                    return 1.0 / (parBump - parFlat);
                case STIRFuture st:
                    return 1.0 / ((parBump - parFlat) / 0.01 * st.UnitPV01);
                case ForwardRateAgreement fra:
                    return 1.0 / ((parBump - parFlat) * fra.FlowScheduleFra.Flows.First().NotionalByYearFraction);
                case ContangoSwap cs:
                    var t360 = (cs.PillarDate - model.BuildDate).TotalDays / 360.0;
                    var spot = model.GetFxRate(model.BuildDate,cs.MetalCCY,cs.CashCCY);
                    var fwdFlat = (1 + parFlat * t360)* spot;
                    var fwdBumped = (1 + parBump * t360) * spot;
                    return 1.0 / (fwdBumped - fwdFlat);
                case IrSwap irs:
                    var pv = irs.SetParRate(parBump).Pv(model, true);
                    return 1.0 / pv * irs.Notional;
                case FloatingRateLoanDepo fld:
                    var pvF = fld.SetParRate(parBump).Pv(model, true);
                    return 1.0 / pvF * fld.Notional;
                default:
                    return 1.0;
            }
        }
        private static double GetBumpSize(IFundingInstrument ins)
        {
            switch (ins)
            {
                case STIRFuture st:
                case OISFuture oi:
                    return 0.01;
                default:
                    return 0.0001;
            }
        }
        private static string GetRiskUnits(IFundingInstrument ins)
        {
            switch (ins)
            {
                case STIRFuture st:
                case OISFuture oi:
                    return "Contracts";
                case ForwardRateAgreement fra:
                case FxForward fxf:
                case IrSwap irs:
                case FloatingRateLoanDepo fld:
                    return "Nominal";
                case ContangoSwap cs:
                    return "Oz";
                default:
                    return "PnL";
            }
        }

    }
}
