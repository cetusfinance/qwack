using System.Collections.Generic;
using Qwack.Core.Models;

namespace Qwack.Paths.Regressors
{
    public interface IRequiresPriceEstimators
    {
        List<ForwardPriceEstimatorSpec> GetRequiredEstimators(IAssetFxModel vanillaModel);
    }
}
