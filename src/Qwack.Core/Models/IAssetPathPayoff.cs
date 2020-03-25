using Qwack.Core.Basic;
using Qwack.Core.Instruments;

namespace Qwack.Core.Models
{
    public interface IAssetPathPayoff
    {
        IAssetInstrument AssetInstrument { get; }
        double AverageResult { get; }
        bool IsComplete { get; }
        double ResultStdError { get; }

        CashFlowSchedule ExpectedFlows(IAssetFxModel model, Currency repCcy = null);
        CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model, Currency repCcy = null);
        void Finish(IFeatureCollection collection);
        void Process(IPathBlock block);
        void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection);

        string RegressionKey { get; }
    }
}
