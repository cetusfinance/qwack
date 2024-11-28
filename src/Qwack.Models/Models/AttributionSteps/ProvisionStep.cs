using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class ProvisionStep(Dictionary<string, double> startProvisions, Dictionary<string, double> endProvisions) : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        startProvisions ??= [];
        endProvisions ??= [];

        HashSet<string> seen = [];

        foreach (var kv in endProvisions)
        {
            var isNew = !startProvisions.TryGetValue(kv.Key, out var start);
            if (isNew)
                start = 0;
            var change = kv.Value - start;
            if (change != 0)
            {
                var underlying = string.Empty;
                var insObj = endModel.Portfolio.Instruments.Where(x=>x.TradeId == kv.Key).FirstOrDefault();
                if(insObj != null)
                {
                    if(insObj is MultiPeriodBackpricingOption mbp)
                        underlying = mbp.AssetId.ToString();
                    else if (insObj is AsianLookbackOption alb)
                        underlying = alb.AssetId.ToString();
                }
                var row = new Dictionary<string, object>
                {
                    { TradeId,  kv.Key},
                    { TradeType, string.Empty },
                    { Step, "Provision" },
                    { SubStep, isNew ? "New" : "Update" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { PointDate, endModel.VanillaModel.BuildDate },
                    { Underlying, underlying }
                };
                resultsCube.AddRow(row, change);
            }
            seen.Add(kv.Key);
        }

        foreach (var kv in startProvisions)
        {
            if (seen.Contains(kv.Key))
                continue;

            var underlying = string.Empty;
            var insObj = endModel.Portfolio.Instruments.Where(x => x.TradeId == kv.Key).FirstOrDefault();
            if (insObj != null)
            {
                if (insObj is MultiPeriodBackpricingOption mbp)
                    underlying = mbp.AssetId.ToString();
                else if (insObj is AsianLookbackOption alb)
                    underlying = alb.AssetId.ToString();
            }

            var row = new Dictionary<string, object>
            {
                { TradeId,  kv.Key},
                { TradeType, string.Empty },
                { Step, "Provision" },
                { SubStep, "Close" },
                { SubSubStep, string.Empty },
                { PointLabel, string.Empty },
                { PointDate, endModel.VanillaModel.BuildDate },
                { Underlying, underlying }
            };
            resultsCube.AddRow(row, -kv.Value);
        }
    
        return (lastPvCube, model);
    }
}
