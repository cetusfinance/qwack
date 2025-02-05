using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class ProvisionStep(List<ProvisionRecord> startProvisions, List<ProvisionRecord> endProvisions) : IPnLAttributionStep
{
    public bool UseFv { get; set; }
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        startProvisions ??= [];
        endProvisions ??= [];
        var startDict = startProvisions.ToDictionary(x=>x.TradeId,x=>x);

        HashSet<string> seen = [];

        foreach (var endProv in endProvisions)
        {
            var isNew = !startDict.TryGetValue(endProv.TradeId, out var start);
            if (isNew)
                start = new ProvisionRecord
                {
                    Provision = 0,
                    MetaData = []
                };
            var change = endProv.Provision - start.Provision;
            if (change != 0)
            {
                var row = new Dictionary<string, object>
                {
                    { TradeId,  endProv.TradeId},
                    { TradeType, string.Empty },
                    { Step, "Provision" },
                    { SubStep, isNew ? "New" : "Update" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { PointDate, endModel.VanillaModel.BuildDate },
                    { Underlying, string.Empty }
                };
                foreach(var kv in endProv.MetaData)
                {
                    row[kv.Key] = kv.Value;
                }
                resultsCube.AddRow(row, change);
            }
            seen.Add(endProv.TradeId);
        }

        foreach (var startProv in startProvisions)
        {
            if (seen.Contains(startProv.TradeId))
                continue;

            var row = new Dictionary<string, object>
            {
                { TradeId,  startProv.TradeId},
                { TradeType, string.Empty },
                { Step, "Provision" },
                { SubStep, "Close" },
                { SubSubStep, string.Empty },
                { PointLabel, string.Empty },
                { PointDate, endModel.VanillaModel.BuildDate },
                { Underlying,  string.Empty  }
            };
            foreach (var kv in startProv.MetaData)
            {
                row[kv.Key] = kv.Value;
            }
            resultsCube.AddRow(row, -startProv.Provision);
        }
    
        return (lastPvCube, model);
    }
}
