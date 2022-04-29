using Qwack.Core.Models;

namespace Qwack.Random.Constant
{
    /// <summary>
    /// Returns a constant number under the guise of being a random number generator
    /// </summary>
    public class Constant : IPathProcess
    {

        public readonly double ReturnValue = 0.5;

        public Constant()
        { }

        public Constant(double returnValue) : base() => ReturnValue = returnValue;

        public bool UseNormalInverse { get; set; }

        public unsafe void Process(IPathBlock block)
        {
            if (!UseNormalInverse)
            {
                for (var i = 0; i < block.TotalBlockSize; i++)
                {
                    block[i] = ReturnValue;
                }
            }
            else
            {
                for (var i = 0; i < block.TotalBlockSize; i++)
                {
                    block[i] = Math.Statistics.NormInv(ReturnValue);
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {

        }
    }
}
