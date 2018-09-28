using System.Collections.Generic;

namespace Qwack.Core.Models
{
    public interface IFeatureCollection
    {
        void AddFeature<T>(T featureToAdd);
        void FinishSetup(List<IRequiresFinish> unfinishedFeatures);
        T GetFeature<T>() where T : class;
    }
}
