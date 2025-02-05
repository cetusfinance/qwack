using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Models.MCModels;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class FxSpotsStep : IPnLAttributionStep
{
    public bool UseFv { get; set; }
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var r_tidIx = riskCube.GetColumnIndex(TradeId);
        var r_plIx = riskCube.GetColumnIndex(PointLabel);
        var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
        var r_pdIx = riskCube.GetColumnIndex(PointDate);
        var r_UlIx = riskCube.GetColumnIndex(Underlying);

        foreach (var fxSpot in endModel.VanillaModel.FundingModel.FxMatrix.SpotRates)
        {
            var fxPair = $"{endModel.VanillaModel.FundingModel.FxMatrix.BaseCurrency.Ccy}/{fxSpot.Key.Ccy}";

            //delta
            var riskForCurve = riskCube.Filter(
                new Dictionary<string, object> {
                        { AssetId, fxPair },
                        { Metric, "FxSpotDeltaT1" }
                });
            var explainedByTrade = new Dictionary<string, double>();
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var startRate = model.VanillaModel.FundingModel.FxMatrix.SpotRates[fxSpot.Key];
                var endRate = fxSpot.Value;
                var explained = r.Value * (endRate - startRate);

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "FxSpots" },
                        { SubStep, fxPair },
                        { SubSubStep, "Delta" },
                        { PointLabel, string.Empty },
                        { PointDate, endModel.VanillaModel.BuildDate },
                        { Underlying, r_UlIx<0 ? string.Empty : r.MetaData[r_UlIx] }
                    };
                resultsCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            //gamma
            riskForCurve = riskCube.Filter(
               new Dictionary<string, object> {
                        { AssetId, fxPair },
                        { Metric, "FxSpotGammaT1" }
               });
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var startRate = model.VanillaModel.FundingModel.FxMatrix.SpotRates[fxSpot.Key];
                var endRate = fxSpot.Value;
                var explained = r.Value * (endRate - startRate) * (endRate - startRate) * 0.5;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "FxSpots" },
                        { SubStep, fxPair },
                        { SubSubStep, "Gamma" },
                        { PointLabel, string.Empty },
                        { PointDate, endModel.VanillaModel.BuildDate },
                        { Underlying, r_UlIx < 0 ? string.Empty : r.MetaData[r_UlIx] }
                    };
                resultsCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            model.VanillaModel.FundingModel.FxMatrix.SpotRates[fxSpot.Key] = fxSpot.Value;
            model = model.Rebuild(model.VanillaModel, model.Portfolio);
            var newPVCube = UseFv ? model.FV(reportingCcy) : model.PV(reportingCcy);
            var step = newPVCube.QuickDifference(lastPvCube);

            foreach (var r in step.GetAllRows())
            {
                if (r.Value == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "FxSpots" },
                        { SubStep, fxPair },
                        { SubSubStep, "Unexplained" },
                        { PointLabel, "Unexplained" },
                        { PointDate, endModel.VanillaModel.BuildDate },
                        { Underlying, r_UlIx<0 ? string.Empty : r.MetaData[r_UlIx] }
                    };
                explainedByTrade.TryGetValue((string)r.MetaData[r_tidIx], out var explained);
                resultsCube.AddRow(row, r.Value - explained);
            }
            lastPvCube = newPVCube;
        }

        return (lastPvCube, model);
    }
}
