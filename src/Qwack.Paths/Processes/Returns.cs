using Qwack.Paths.Features;
using System.Numerics;
using Qwack.Core.Models;

namespace Qwack.Paths.Processes
{
    public class Returns : IPathProcess
    {
        private readonly string _assetName;
        private int _assetIndex;
        private int _outputIndex;
        private readonly string _name = "Returns";
        private readonly bool _logReturns;
        private readonly Vector<double> _two = new Vector<double>(2.0);

        public Returns(string assetName, string outputName, bool logReturns)
        {
            _name = outputName;
            _assetName = assetName;
            _logReturns = logReturns;
        }

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);
            _outputIndex = dims.GetDimension(_name);
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var previousIndex = new double[Vector<double>.Count];
                steps[0].CopyTo(previousIndex);
                steps[0] = new Vector<double>(0);
                for (var step = 1; step < block.NumberOfSteps; step++)
                {
                    var currentIndex = new double[Vector<double>.Count];
                    steps[step].CopyTo(currentIndex);

                    if(_logReturns)
                    {
                        
                    }
                    else
                    {
                        steps[step] = new Vector<double>(currentIndex) / new Vector<double>(previousIndex) - new Vector<double>(1.0);
                    }
                    previousIndex = currentIndex;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
        }
    }
}
