using System.Numerics;
using Qwack.Core.Models;

namespace Qwack.Random.Constant
{
    /// <summary>
    /// Returns a constant number, flipping from positive to negative on alternate paths, 
    /// under the guise of being a random number generator
    /// </summary>
    public class FlipFlop : IPathProcess
    {

        public readonly double ReturnValue = 0.5;
        public Vector<double> _returnVec;
        public Vector<double> _returnVecNormInv;

        public FlipFlop() => Init();

        public FlipFlop(double returnValue, bool useNormalInverse) : base()
        {
            ReturnValue = returnValue;
            UseNormalInverse = useNormalInverse;
            Init();
        }

        private void Init()
        {
            var r = new double[Vector<double>.Count];
            for (var i = 0; i < Vector<double>.Count; i++)
            {
                r[i] = (i % 2 == 0 ? 1 : -1) * (UseNormalInverse ? Math.Statistics.NormInv(ReturnValue) : ReturnValue);
            }
            _returnVec = new Vector<double>(r);
        }

        public bool UseNormalInverse { get; }

        public unsafe void Process(IPathBlock block)
        {
            for (var f = 0; f < block.Factors; f++)
            {
                for (var p = 0; p < block.NumberOfPaths; p += Vector<double>.Count)
                {
                    var s = block.GetStepsForFactor(p, f);

                    for (var i = 0; i < block.NumberOfSteps; i++)
                    {
                        s[i] = _returnVec;
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {

        }
    }
}
