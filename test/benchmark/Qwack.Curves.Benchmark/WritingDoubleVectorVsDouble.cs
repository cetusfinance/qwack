using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Qwack.Core.Models;
using Qwack.Paths;
using Qwack.Paths.Features;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(QuickSpinConfig))]
    public class WritingDoubleVectorVsDouble
    {
        
        [Benchmark(Baseline = true)]
        public void RandsWithNoVectors()
        {
            var Paths = (int)System.Math.Pow(2, 14);
            using (var engine = new PathEngine(Paths))
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
            var Paths = (int)System.Math.Pow(2, 14);
            using (var engine = new PathEngine(Paths))
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

        //[Benchmark()]
        //public void RandsWithVectorsNormInv()
        //{
        //    var Paths = (int)System.Math.Pow(2, 14);
        //    using (var engine = new PathEngine(Paths))
        //    {
        //        engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
        //        {
        //            UseNormalInverse = true,
        //            UseVectorWrite = true
        //        });
        //        engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
        //        engine.SetupFeatures();
        //        engine.RunProcess();
        //    }
        //}

        //[Benchmark()]
        //public void RandsWithVectors()
        //{
        //    var Paths = (int)System.Math.Pow(2, 14);
        //    using (var engine = new PathEngine(Paths))
        //    {
        //        engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
        //        {
        //            UseVectorWrite = true
        //        });
        //        engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
        //        engine.SetupFeatures();
        //        engine.RunProcess();
        //    }
        //}

        //[Benchmark()]
        //public void RandsPointersNormInv()
        //{
        //    var Paths = (int)System.Math.Pow(2, 14);
        //    using (var engine = new PathEngine(Paths))
        //    {
        //        engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
        //        {
        //            UseNormalInverse = true,
        //            UseVectorWrite = true,
        //            UsePointerWrite = true
        //        });
        //        engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
        //        engine.SetupFeatures();
        //        engine.RunProcess();
        //    }
        //}

        //[Benchmark()]
        //public void RandsPointers()
        //{
        //    var Paths = (int)System.Math.Pow(2, 14);
        //    using (var engine = new PathEngine(Paths))
        //    {
        //        engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
        //        {
        //            UseVectorWrite = true,
        //            UsePointerWrite = true
        //        });
        //        engine.AddPathProcess(new FakeAssetProcess("TestUnderlying", numberOfDimensions: 2, timesteps: 100));
        //        engine.SetupFeatures();
        //        engine.RunProcess();
        //    }
        //} 

        private class FakeAssetProcess : IPathProcess
        {
            private readonly string _name;
            private readonly int _numberOfDimensions;
            private readonly int _timesteps;

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

            public void Process(IPathBlock block)
            {
                throw new NotImplementedException();
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

            public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
            {
                throw new NotImplementedException();
            }
        }
    }
}
