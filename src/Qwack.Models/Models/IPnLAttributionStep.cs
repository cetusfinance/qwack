using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Models.MCModels;

namespace Qwack.Models.Models;

public interface IPnLAttributionStep
{
    public ICube Attribute(AssetFxMCModel model, AssetFxMCModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy);

}
