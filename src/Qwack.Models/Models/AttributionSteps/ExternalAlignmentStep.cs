using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class ExternalAlignmentStep(List<ProvisionRecord> externalPnLs) : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        externalPnLs ??= [];

        var pnlByTrade = resultsCube
            .Pivot(TradeId, AggregationAction.Sum)
            .GetAllRows()
            .ToDictionary(x => x.MetaData[0] ?? "Unknown", x => x.Value);

        foreach (var p in externalPnLs)
        {

            var ux = 0.0;
            if(pnlByTrade.TryGetValue(p.TradeId, out var pnlForTradeId))
            {
                ux = p.Provision - pnlForTradeId;
            }
            else
            {
                ux = p.Provision;
            }
            if (System.Math.Round(ux,2) != 0)
            {
                var row = new Dictionary<string, object>
                {
                    { TradeId,  p.TradeId},
                    { TradeType, string.Empty },
                    { Step, "Align" },
                    { SubStep, "External" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { PointDate, endModel.VanillaModel.BuildDate },
                    { Underlying, string.Empty }
                };
                foreach (var kv in p.MetaData)
                {
                    row[kv.Key] = kv.Value;
                }
                resultsCube.AddRow(row, ux);
            }
        }

        return (lastPvCube, model);
    }
}
