using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;

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

            var lastDate = pvModel.Portfolio.LastSensitivityDate();

            var insByCurve = riskCollection.GroupBy(x => x.SolveCurve);
            var insToRisk = new List<IFundingInstrument>();
            foreach(var gp in insByCurve)
            {
                var sorted = gp.OrderBy(x => x.LastSensitivityDate).ToList();
                if (sorted.Last().LastSensitivityDate <= lastDate)
                    insToRisk.AddRange(sorted);
                else
                {
                    var lastIns = sorted.First(x => x.LastSensitivityDate > lastDate);
                    var lastIx = sorted.IndexOf(lastIns);
                    lastIx = System.Math.Min(lastIx + 2, sorted.Count);
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
         
            for (var i = 0; i < newIns.Count(); i++)
            {
                var basePV = newPvModel.PV(insToRisk[i].Currency);
                var tIdIx = basePV.GetColumnIndex("TradeId");
                var tTypeIx = basePV.GetColumnIndex("TradeType");

                var bumpSize = GetBumpSize(insToRisk[i]);

                var bumpedIns = insToRisk.Select((x, ix) => x.SetParRate(parRates[ix] + (ix == i ? bumpSize : 0.0)));
                var newFicb = new FundingInstrumentCollection(currencyProvider);
                newFicb.AddRange(bumpedIns);

                var fModelb = fModel.DeepClone(null);

                var sb = new NewtonRaphsonMultiCurveSolverStaged();
                sb.Solve(fModelb, newFicb);

                var vModelb = pvModel.VanillaModel.Clone(fModelb);
                var newPvModelb = pvModel.Rebuild(vModelb, pvModel.Portfolio);

                var bumpedPV = newPvModelb.PV(insToRisk[i].Currency);
                var bumpName = insToRisk[i].TradeId;
                var riskDate = insToRisk[i].PillarDate;
                var riskCurve = insToRisk[i].SolveCurve;
                var riskUnits = GetRiskUnits(insToRisk[i]);

                var deltaCube = bumpedPV.QuickDifference(basePV);
                var deltaScale = GetScaleFactor(insToRisk[i], parRates[i], parRates[i] + bumpSize);

                foreach (var dRow in deltaCube.GetAllRows())
                {
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
                    cube.AddRow(row, dRow.Value * deltaScale);
                }
            }

            return cube.Sort();
        }

        private static double GetScaleFactor(IFundingInstrument ins, double parFlat, double parBump)
        {
            switch (ins)
            {
                case FxForward fxf:
                    return 1.0 / (parBump - parFlat);
                case STIRFuture st:
                    return -1.0 / ((parBump - parFlat) / 0.01 * st.UnitPV01);
                case ForwardRateAgreement fra:
                    return 1.0 / ((parBump - parFlat) * fra.FlowScheduleFra.Flows.First().NotionalByYearFraction);
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
                    return "Nominal";
                default:
                    return "PnL";
            }
        }

    }
}
