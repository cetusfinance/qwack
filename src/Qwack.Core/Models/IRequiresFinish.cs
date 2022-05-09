namespace Qwack.Core.Models
{
    public interface IRequiresFinish
    {
        void Finish(IFeatureCollection collection);
        bool IsComplete { get; }
    }
}
