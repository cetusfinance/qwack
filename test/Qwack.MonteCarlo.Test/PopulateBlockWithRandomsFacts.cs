using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Paths;
using Qwack.Paths.Features;
using Xunit;

namespace Qwack.MonteCarlo.Test
{
    public class PopulateBlockWithRandomsFacts
    {
        [Fact]
        public void TestBlockGeneration()
        {
            var engine = new PathEngine(4 << 2);
            engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64());
            engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", 2, 100));
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

            public void Process(PathBlock block)
            {
                //NoOp
            }

            public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
            {
                var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
                for (int i = 0; i < _numberOfDimensions; i++)
                {
                    mappingFeature.AddDimension($"{_name}-{i}");
                }
                var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
                for (int i = 0; i < _timesteps; i++)
                {
                    dates.AddDate(DateTime.Now.Date.AddDays(i));
                }
            }
        }
    }
}
