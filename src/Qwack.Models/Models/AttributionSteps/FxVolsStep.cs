using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Models.MCModels;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class FxVolsStep : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var r_tidIx = riskCube.GetColumnIndex(TradeId);
        var r_plIx = riskCube.GetColumnIndex(PointLabel);
        var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
        var r_pdIx = riskCube.GetColumnIndex("PointDate");

        foreach (var fxSurface in endModel.VanillaModel.FundingModel.VolSurfaces)
        {
            var riskForCurve = riskCube.Filter(
                 new Dictionary<string, object> {
                        { AssetId, fxSurface.Key },
                        { Metric, "Vega" }
                 });

            var explainedByTrade = new Dictionary<string, double>();
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var point = (string)r.MetaData[r_plIx];
                var pointDate = fxSurface.Value.PillarDatesForLabel(point);
                var startRate = model.VanillaModel.GetFxVolForDeltaStrikeAndDate(fxSurface.Key, pointDate, 0.5);
                var endRate = endModel.VanillaModel.GetFxVolForDeltaStrikeAndDate(fxSurface.Key, pointDate, 0.5);
                var explained = r.Value * (endRate - startRate) / 0.01;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "FxVols" },
                        { SubStep, fxSurface.Key },
                        { SubSubStep, "Vega" },
                        { PointLabel,r.MetaData[r_plIx]},
                        { "PointDate",r.MetaData[r_pdIx] }
                    };
                riskCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            model.VanillaModel.FundingModel.VolSurfaces[fxSurface.Key] = fxSurface.Value;
            model = model.Rebuild(model.VanillaModel, model.Portfolio);
            var newPVCube = model.PV(reportingCcy);
            var step = newPVCube.QuickDifference(lastPvCube);

            foreach (var r in step.GetAllRows())
            {
                if (r.Value == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "FxVols" },
                        { SubStep, fxSurface.Key },
                        { SubSubStep, "Unexplained" },
                        { PointLabel, "Unexplained" },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                explainedByTrade.TryGetValue((string)r.MetaData[r_tidIx], out var explained);
                resultsCube.AddRow(row, r.Value - explained);
            }
            lastPvCube = newPVCube;
        }

        return (lastPvCube, model);
    }
}
