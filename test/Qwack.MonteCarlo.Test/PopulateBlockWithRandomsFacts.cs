using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Paths;
using Qwack.Paths.Features;
using Xunit;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Qwack.MonteCarlo.Test
{
    public class PopulateBlockWithRandomsFacts
    {
        [Fact]
        public void TestBlockGeneration()
        {
            var engine = new PathEngine(4 << 2);
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64());
            engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 10));
            engine.SetupFeatures();
            engine.RunProcess();
        }

        private class FakeAssetProcess : IPathProcess
        {
            private string _name;
            private int _numberOfDimensions;
            private int _timesteps;

            public FakeAssetProcess(string name, int numberOfDimensions, int timesteps)
            {
                _name = name;
                _timesteps = timesteps;
                _numberOfDimensions = numberOfDimensions;
            }

            public unsafe void Process(PathBlock block)
            {
                var currentIndex = 0 * block.NumberOfPaths * block.NumberOfSteps;
                for(var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    //This should be set to the spot price here
                    var previousStep = Vector<double>.One;
                    for(var step = 0; step < block.NumberOfSteps;step++)
                    {
                        ref Vector<double> currentValue = ref block.ReadVectorByRef(currentIndex);


                        currentValue = Vector.Multiply(previousStep, currentValue);
                        previousStep = currentValue;
                        currentIndex += Vector<double>.Count;
                    }
                }
            }

            public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
            {
                var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
                for (var i = 0; i < _numberOfDimensions; i++)
                {
                    mappingFeature.AddDimension($"{_name}-{i}");
                }
                var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
                for (var i = 0; i < _timesteps; i++)
                {
                    dates.AddDate(DateTime.Now.Date.AddDays(i));
                }
            }
        }
    }
}
