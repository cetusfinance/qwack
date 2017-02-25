using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Qwack.Paths;
using Qwack.Paths.Features;

namespace Qwack.Curves.Benchmark
{
    public class WritingDoubleVectorVsDouble : QuickSpinConfig
    {
        [Benchmark(Baseline = true)]
        public void RandsWithNoVectors()
        {
            using (var engine = new PathEngine(2 ^ 12))
            {
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64());
                engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
                engine.SetupFeatures();
                engine.RunProcess();
            }
        }

        [Benchmark()]
        public void RandsWithNoVectorsNormInv()
        {
            using (var engine = new PathEngine(2 ^ 12))
            {
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true
                });
                engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
                engine.SetupFeatures();
                engine.RunProcess();
            }
        }

        [Benchmark()]
        public void RandsWithVectorsNormInv()
        {
            using (var engine = new PathEngine(2 ^ 12))
            {
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseNormalInverse = true,
                    UseVectorWrite = true
                });
                engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
                engine.SetupFeatures();
                engine.RunProcess();
            }
        }

        [Benchmark()]
        public void RandsWithVectors()
        {
            using (var engine = new PathEngine(2 ^ 12))
            {
                engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                {
                    UseVectorWrite = true
                });
                engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
                engine.SetupFeatures();
                engine.RunProcess();
            }
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
