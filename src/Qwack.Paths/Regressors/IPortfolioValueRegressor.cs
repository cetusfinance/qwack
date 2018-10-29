using Qwack.Core.Models;
using Qwack.Math.Regression;

namespace Qwack.Paths.Regressors
{
    public interface IPortfolioValueRegressor : IPathProcess, IRequiresFinish
    {
        double[] PFE(IAssetFxModel model, double confidenceInterval);
    }
}
