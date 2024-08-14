using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class DeltaGammaCurveStep : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        foreach (var curveName in endModel.VanillaModel.CurveNames)
        {
            var riskForCurve = riskCube.Filter(
                new Dictionary<string, object> { { AssetId, curveName }, { Metric, "AssetDeltaT1" } });
            var riskForCurveGamma = riskCube.Filter(
                new Dictionary<string, object> { { AssetId, curveName }, { Metric, "AssetGammaT1" } });

            var r_tidIx = riskCube.GetColumnIndex(TradeId);
            var r_plIx = riskCube.GetColumnIndex(PointLabel);
            var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
            var r_pdIx = riskCube.GetColumnIndex(PointDate);
            var r_UlIx = riskCube.GetColumnIndex(Underlying);

            var startCurve = model.VanillaModel.GetPriceCurve(curveName);
            var endCurve = endModel.VanillaModel.GetPriceCurve(curveName);

            var fxRate = model.VanillaModel.FundingModel.GetFxRate(model.VanillaModel.BuildDate, startCurve.Currency, reportingCcy);

            var explainedByTrade = new Dictionary<string, double>();
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var point = (string)r.MetaData[r_plIx];

                var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var move = (endRate - startRate);
                var explained = r.Value * move * fxRate;

                var row = new Dictionary<string, object>
                {
                    { TradeId, r.MetaData[r_tidIx] },
                    { TradeType, r.MetaData[r_tTypeIx] },
                    { Step, "AssetCurves" },
                    { SubStep, curveName },
                    { SubSubStep, "Delta" },
                    { PointLabel, r.MetaData[r_plIx] },
                    { PointDate, r.MetaData[r_pdIx] },
                    { Underlying, r_UlIx<0 ? string.Empty : r.MetaData[r_UlIx] }
                };
                resultsCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            foreach (var r in riskForCurveGamma.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var point = (string)r.MetaData[r_plIx];

                var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var move = (endRate - startRate);
                var explained = 0.5 * r.Value * move * move * fxRate;


                var row = new Dictionary<string, object>
                {
                    { TradeId, r.MetaData[r_tidIx] },
                    { TradeType, r.MetaData[r_tTypeIx] },
                    { Step, "AssetCurves" },
                    { SubStep, curveName },
                    { SubSubStep, "Gamma" },
                    { PointLabel, r.MetaData[r_plIx] },
                    { PointDate, r.MetaData[r_pdIx] },
                    { Underlying, r_UlIx<0 ? string.Empty : r.MetaData[r_UlIx] }
                };
                resultsCube.AddRow(row, explained);

                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
            }

            model.VanillaModel.AddPriceCurve(curveName, endModel.VanillaModel.GetPriceCurve(curveName));
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
                    { Step, "AssetCurves" },
                    { SubStep, curveName },
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
