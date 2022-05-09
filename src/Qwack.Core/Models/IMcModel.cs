using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;

namespace Qwack.Core.Models
{
    public interface IPvModel
    {
        ICube PV(Currency reportingCurrency);
        IAssetFxModel VanillaModel { get; }
        IPvModel Rebuild(IAssetFxModel newVanillaModel, Portfolio portfolio);

        Portfolio Portfolio { get; }
    }
}
