using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Core.Cubes;

namespace Qwack.Models.Risk
{
    public class BenchmarkCurveRisk
    {
        public static ICube BenchmarkRisk(IPvModel pvModel, FundingInstrumentCollection riskCollection, ICurrencyProvider currencyProvider, Currency reportingCcy)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType",  typeof(string) },
                { "Benchmark", typeof(string) },
                { "Metric", typeof(string) },
            };
            cube.Initialize(dataTypes);


            var parRates = riskCollection.Select(x => x.CalculateParRate(pvModel.VanillaModel.FundingModel)).ToList();
            var newIns = riskCollection.Select((x, ix) => x.SetParRate(parRates[ix]));
            var newFic = new FundingInstrumentCollection(currencyProvider);
            newFic.AddRange(newIns);

            var fModel = pvModel.VanillaModel.FundingModel.DeepClone(null);
            var s = new NewtonRaphsonMultiCurveSolverStaged();
            s.Solve(fModel, newFic);
            
            var vModel = pvModel.VanillaModel.Clone(fModel);
            var newPvModel = pvModel.Rebuild(vModel, pvModel.Portfolio);

            var basePV = newPvModel.PV(reportingCcy);
            var tIdIx = basePV.GetColumnIndex("TradeId");
            var tTypeIx = basePV.GetColumnIndex("TradeType");

            var bumpSize = 0.0001;

            for (var i = 0; i < newIns.Count(); i++)
            {
                var bumpedIns = riskCollection.Select((x, ix) => x.SetParRate(parRates[ix] + (ix == i ? bumpSize : 0.0)));
                var newFicb = new FundingInstrumentCollection(currencyProvider);
                newFicb.AddRange(bumpedIns);

                var fModelb = pvModel.VanillaModel.FundingModel.DeepClone(null);

                var sb = new NewtonRaphsonMultiCurveSolverStaged();
                sb.Solve(fModelb, newFicb);

                var vModelb = pvModel.VanillaModel.Clone(fModel);
                var newPvModelb = pvModel.Rebuild(vModelb, pvModel.Portfolio);

                var bumpedPV = newPvModelb.PV(reportingCcy);
                var bumpName = riskCollection[i].TradeId;
                var deltaCube = bumpedPV.QuickDifference(basePV);
               
                foreach (var dRow in deltaCube.GetAllRows())
                {
                    var row = new Dictionary<string, object>
                            {
                                { "TradeId", dRow.MetaData[tIdIx] },
                                { "TradeType", dRow.MetaData[tTypeIx] },
                                { "Benchmark", bumpName },
                                { "Metric", "IrBenchmarkDelta" }
                            };
                    cube.AddRow(row, dRow.Value);
                }
            }

            return cube.Sort();
        }

    }
}
