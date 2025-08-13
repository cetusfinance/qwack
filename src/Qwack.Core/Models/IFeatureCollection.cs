using System.Collections.Generic;

namespace Qwack.Core.Models
{
    public interface IFeatureCollection
    {
        void AddFeature<T>(T featureToAdd);
        void FinishSetup(List<IRequiresFinish> unfinishedFeatures);
        T GetFeature<T>() where T : class;

        void RegisterRequiredFeature<T>(string category, T feature) where T : class;
        bool HasRequiredFeatureRegistration<T>(string category, T feature) where T : class;
        void UpdateRequiredFeature<T>(string category, T feature) where T : class;
        T GetRequiredFeature<T>(string category, T feature) where T : class;
        T[] GetRequiredFeatures<T>(string category) where T : class;

        void AddPriceEstimator(ForwardPriceEstimatorSpec spec, IForwardPriceEstimate estimator);
        IForwardPriceEstimate GetPriceEstimator(ForwardPriceEstimatorSpec spec);
    }
}
