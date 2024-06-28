using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Models.MCModels;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class AtmVegaCurveStep : IPnLAttributionStep
{
    public ICube Attribute(AssetFxMCModel model, AssetFxMCModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
      foreach (var surfaceName in endModel.VanillaModel.VolSurfaceNames)
            {
                var riskForCurve = riskCube.Filter(
                      new Dictionary<string, object> {
                        { AssetId, surfaceName },
                        { Metric, "Vega" }
                      });
                
                var r_tidIx = riskCube.GetColumnIndex(TradeId);
                var r_plIx = riskCube.GetColumnIndex(PointLabel);
                var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
                var r_pdIx = riskCube.GetColumnIndex("PointDate");

                var startCurve = model.VanillaModel.GetVolSurface(surfaceName);
                var endCurve = endModel.VanillaModel.GetVolSurface(surfaceName);
                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = startCurve.PillarDatesForLabel(point);
                    var startRate = model.VanillaModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var endRate = endModel.VanillaModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.01;

                    var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "AssetVols" },
                        { SubStep, surfaceName },
                        { SubSubStep, "Vega" },
                        { PointLabel, r.MetaData[r_plIx] },
                        { "PointDate", r.MetaData[r_pdIx] }
                    };
                    resultsCube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                var targetSurface = endModel.VanillaModel.GetVolSurface(surfaceName);
                model.VanillaModel.AddVolSurface(surfaceName, targetSurface);
                model = (AssetFxMCModel)model.Rebuild(model.VanillaModel, model.Portfolio);
                var newPvCube = model.PV(reportingCcy);
                var step = newPvCube.QuickDifference(lastPvCube);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "AssetVols" },
                        { SubStep, surfaceName },
                        { SubSubStep, "Unexplained" },
                        { PointLabel, "Unexplained" },
                        { "PointDate", endModel.OriginDate }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[r_tidIx], out var explained);
                    resultsCube.AddRow(row, r.Value - explained);
                }
                lastPvCube = newPvCube;
            }

        return lastPvCube;
    }
}
