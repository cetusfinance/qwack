using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class FinalStep() : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var newPvCube = endModel.PV(reportingCcy);
        var step = newPvCube.QuickDifference(lastPvCube);

        foreach (var r in step.GetAllRows())
        {
            if (r.Value == 0.0) continue;

            var tidIx = newPvCube.GetColumnIndex(TradeId);
            var tTypeIx = newPvCube.GetColumnIndex(TradeType);

            var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[tidIx] },
                        { TradeType, r.MetaData[tTypeIx] },
                        { Step, "Unexplained" },
                        { SubStep, "Unexplained" },
                        { SubSubStep, "Unexplained" },
                        { PointLabel, string.Empty },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
            resultsCube.AddRow(row, r.Value);
        }
        return (newPvCube, model);
    }
}
