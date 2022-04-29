using Qwack.Core.Models;

namespace Qwack.Paths.Features.Rates
{
    public static class RatesExtensions
    {
        public static void AddRate<T>(this PathEngine engine, T rateFeature) where T : IRateFeature
        {
            var rateCollection = engine.Features.GetFeature<IRatesFeature>();
            rateCollection.AddRate(rateFeature);
            if (rateFeature is IPathProcess process)
            {
                engine.AddPathProcess(process);
            }
        }
    }
}
