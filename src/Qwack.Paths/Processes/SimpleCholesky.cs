using Qwack.Paths.Features;
using System.Numerics;
using Qwack.Core.Models;

namespace Qwack.Paths.Processes
{
    public class SimpleCholesky : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;
        //private readonly string _name = "Cholesky";
        private readonly double _fac2;
        private readonly double _correl;
        private readonly Vector<double> _two = new Vector<double>(2.0);

        public SimpleCholesky(double correl)
        {
            _correl = correl;
            _fac2 = System.Math.Sqrt(1.0 - correl * correl);
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection) => _isComplete = true;

        public void Process(IPathBlock block)
        {
            var nFactors = block.Factors;

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var randsForSteps = new Vector<double>[nFactors][];
                for(var r=0;r< randsForSteps.Length;r++)
                {
                    randsForSteps[r] = block.GetStepsForFactor(path, r).ToArray();
                }

                for (var step = 0; step < block.NumberOfSteps; step++)
                {
                    var randsForThisStep = new Vector<double>[nFactors];

                    for (var r = 0; r < randsForThisStep.Length; r++)
                    {
                        randsForThisStep[r] = randsForSteps[r][step];
                    }

                    var correlatedRands = Correlate(randsForThisStep);

                    for (var r = 0; r < randsForSteps.Length; r++)
                    {
                        var x = block.GetStepsForFactor(path, r);
                        x[step] = correlatedRands[r];
                    }
                }
            }
        }

        private double[] Correlate(double[] rands)
        {
            var returnValues = new double[rands.Length];
            returnValues[0] = rands[0];
            returnValues[1] = rands[0] * _correl + rands[1] * _fac2;
            return returnValues;
        }

        private Vector<double>[] Correlate(Vector<double>[] rands)
        {
            var returnValues = new Vector<double>[rands.Length];
            returnValues[0] = rands[0];
            returnValues[1] = rands[0] * _correl + rands[1] * _fac2;
            return returnValues;
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
        }
    }
}
