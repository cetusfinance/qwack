using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Models.MCModels;

namespace Qwack.Models.Models;

public interface IPnLAttributionStep
{
    public bool UseFv { get; set; }
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy);

}
