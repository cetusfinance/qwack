namespace Qwack.Core.Models
{
    public interface IPathProcess
    {
        void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection);
        void Process(IPathBlock block);
    }
}
