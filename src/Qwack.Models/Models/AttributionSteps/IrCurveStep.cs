using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class IrCurveStep : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var r_tidIx = riskCube.GetColumnIndex(TradeId);
        var r_plIx = riskCube.GetColumnIndex(PointLabel);
        var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
        var r_pdIx = riskCube.GetColumnIndex("PointDate");

        foreach (var irCurve in endModel.VanillaModel.FundingModel.Curves)
        {
            var riskForCurve = riskCube.Filter(
                new Dictionary<string, object> {
                        { AssetId, irCurve.Key },
                        { Metric, "IrDelta" }
                });

            var explainedByTrade = new Dictionary<string, double>();
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var point = DateTime.Parse((string)r.MetaData[r_plIx]);
                var startRate = model.VanillaModel.FundingModel.Curves[irCurve.Key].GetRate(point);
                var endRate = irCurve.Value.GetRate(point);
                var explained = r.Value * (endRate - startRate) / 0.0001;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "IrCurves" },
                        { SubStep, irCurve.Key },
                        { SubSubStep, string.Empty },
                        { PointLabel,r.MetaData[r_plIx]},
                        { "PointDate", point }
                    };
                resultsCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            model.VanillaModel.FundingModel.Curves[irCurve.Key] = irCurve.Value;
            model = model.Rebuild(model.VanillaModel, model.Portfolio);
            var newPvCube = model.PV(reportingCcy);
            var step = newPvCube.QuickDifference(lastPvCube);

            foreach (var r in step.GetAllRows())
            {
                if (explainedByTrade.TryGetValue((string)r.MetaData[r_tidIx], out var explained))
                {
                    explainedByTrade.Remove((string)r.MetaData[r_tidIx]);
                }

                if (r.Value - explained == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "IrCurves" },
                        { SubStep, irCurve.Key },
                        { SubSubStep, "Unexplained"},
                        { PointLabel, "Unexplained" },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                resultsCube.AddRow(row, r.Value - explained);
            }

            //overspill
            foreach (var kv in explainedByTrade)
            {
                var row = new Dictionary<string, object>
                    {
                        { TradeId, kv.Key },
                        { TradeType, string.Empty },
                        { Step, "IrCurves" },
                        { SubStep, irCurve.Key },
                        { SubSubStep, "Unexplained"},
                        { PointLabel, "Unexplained" },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                resultsCube.AddRow(row, -kv.Value);
            }

            lastPvCube = newPvCube;
        }

        return (lastPvCube, model);
    }
}
