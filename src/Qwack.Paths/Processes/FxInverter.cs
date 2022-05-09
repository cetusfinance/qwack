using System.Numerics;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Processes
{
    public class FxInverter : IPathProcess, IRequiresFinish, IRunSingleThreaded
    {
        private readonly Vector<double> _one = new(1.0);
        private readonly string _fxPair;
        private readonly string _invertedFxPair;

        private int _fxPairIx;
        private int _invertedFxPairIx;

        private bool _isComplete;

        public FxInverter(string fxPair)
        {
            _fxPair = fxPair;
            _invertedFxPair = _fxPair.Substring(_fxPair.Length - 3, 3) + '/' + _fxPair.Substring(0, 3);
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _fxPairIx = dims.GetDimension(_fxPair);

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _fxPairIx);
                var stepsInv = block.GetStepsForFactor(path, _invertedFxPairIx);
                for (var s = 0; s < steps.Length; s++)
                {
                    stepsInv[s] = _one / steps[s];
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _invertedFxPairIx = mappingFeature.AddDimension(_invertedFxPair);
        }
    }
}

