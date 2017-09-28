using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Underlyings;
using System.Numerics;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;

namespace Qwack.Paths.Processes
{
    public class Cholesky : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;

        private string _name = "Cholesky";
        private double _correlation;
        private double _factor;

        private readonly Vector<double> _two = new Vector<double>(2.0);

        public Cholesky(double correlation)
        {
            if (System.Math.Abs(correlation) > 1.0)
                throw new Exception("Invalid correlation, must be in the range -1.0 to +1.0");
            _correlation = correlation;
            _factor = System.Math.Sqrt(1 - _correlation * _correlation);
        }

        public bool IsComplete => _isComplete;

        public void Finish(FeatureCollection collection)
        {
            _isComplete = true;
        }

        public void Process(PathBlock block)
        {
            if (block.Factors != 2)
                throw new Exception("Expected only 2 factors");

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var r1 = block.GetStepsForFactor(path, 0);
                var r2 = block.GetStepsForFactor(path, 1);

                for (var step = 1; step < block.NumberOfSteps; step++)
                {
                    var W1 = r1[step];
                    var W2 = _correlation * W1 + _factor * r2[step];
                    r1[step] = W1;
                    r2[step] = W2;
                }
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
        }
    }
}
