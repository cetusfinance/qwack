using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class DeltaFlatSpreadGammaCurveStep(bool ignoreGamma = false) : IPnLAttributionStep
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
            var r_pdIx = riskCube.GetColumnIndex("PointDate");

            var startCurve = model.VanillaModel.GetPriceCurve(curveName);
            var endCurve = endModel.VanillaModel.GetPriceCurve(curveName);

            var fxRate = model.VanillaModel.FundingModel.GetFxRate(model.VanillaModel.BuildDate, startCurve.Currency, reportingCcy);

            var explainedByTrade = new Dictionary<string, double>();
            var refStartRate = startCurve.GetPriceForDate(startCurve.RefDate);
            var refEndRate = endCurve.GetPriceForDate(endCurve.RefDate);
            var flatShift = refEndRate - refStartRate;
            foreach (var r in riskForCurve.GetAllRows())
            {
                if (r.Value == 0.0) continue;
                var point = (string)r.MetaData[r_plIx];

                var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                var move = (endRate - startRate);
                var explainedFlat = r.Value * flatShift * fxRate;
                var explainedSpread = r.Value * (move - flatShift) * fxRate;

                var rowFlat = new Dictionary<string, object>
                {
                    { TradeId, r.MetaData[r_tidIx] },
                    { TradeType, r.MetaData[r_tTypeIx] },
                    { Step, "AssetCurves" },
                    { SubStep, curveName },
                    { SubSubStep, "DeltaFlat" },
                    { PointLabel, r.MetaData[r_plIx] },
                    { "PointDate", r.MetaData[r_pdIx] }
                };
                var rowSpread = new Dictionary<string, object>
                {
                    { TradeId, r.MetaData[r_tidIx] },
                    { TradeType, r.MetaData[r_tTypeIx] },
                    { Step, "AssetCurves" },
                    { SubStep, curveName },
                    { SubSubStep, "DeltaSpread" },
                    { PointLabel, r.MetaData[r_plIx] },
                    { "PointDate", r.MetaData[r_pdIx] }
                };

                resultsCube.AddRow(rowFlat, explainedFlat);
                resultsCube.AddRow(rowSpread, explainedSpread);
                
                if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                    explainedByTrade[(string)r.MetaData[r_tidIx]] = explainedFlat + explainedSpread;
                else
                    explainedByTrade[(string)r.MetaData[r_tidIx]] += (explainedFlat + explainedSpread);
            }

            if (!ignoreGamma)
            {
                foreach (var r in riskForCurveGamma.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];

                    //var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    //var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));

                    //var move = (endRate - startRate) / startRate;
                    //var dollarGamma =  r.Value / startRate;
                    //var explained = 0.5 * dollarGamma * startRate * move * move;

                    //var move = (endRate - startRate);
                    //var explained = 0.5 * r.Value * move * move * fxRate;
                    var explained = 0.5 * r.Value * flatShift * flatShift * fxRate;


                    var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[r_tidIx] },
                        { TradeType, r.MetaData[r_tTypeIx] },
                        { Step, "AssetCurves" },
                        { SubStep, curveName },
                        { SubSubStep, "GammaFlat" },
                        { PointLabel, "Flat" },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                    resultsCube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }
            }

            model.VanillaModel.AddPriceCurve(curveName, endModel.VanillaModel.GetPriceCurve(curveName));
            model = model.Rebuild(model.VanillaModel, model.Portfolio);
            var newPvCube = model.PV(reportingCcy);
            var step = newPvCube.QuickDifference(lastPvCube);

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
                    { "PointDate", endModel.VanillaModel.BuildDate }
                };
                explainedByTrade.TryGetValue((string)r.MetaData[r_tidIx], out var explained);
                resultsCube.AddRow(row, r.Value - explained);
            }

            lastPvCube = newPvCube;
        }

        return (lastPvCube, model);
    }
}
