using Qwack.Core.Models;

namespace Qwack.Paths.Regressors
{
    public interface IPortfolioValueRegressor : IPathProcess, IRequiresFinish
    {
        double[] PFE(IAssetFxModel model, double confidenceInterval);
        double[] EPE(IAssetFxModel model);
        double[] ENE(IAssetFxModel model);
    }
}
