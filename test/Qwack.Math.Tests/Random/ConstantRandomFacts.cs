using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Paths;
using Qwack.Random.Constant;
using Qwack.Math.Tests.Random;
using Xunit;
using static Qwack.Math.Tests.Random.SobolFacts;
using Qwack.Paths.Features;

namespace Qwack.Math.Tests.Random
{
    public class ConstantRandomFacts
    {

        [Fact]
        public void TestBlockGeneration()
        {
            var block = new PathBlock(64, 1, 10, 0, 0);
            var retVal = 0.5;
            var gen = new Constant(retVal);
            var fetCollection = new FeatureCollection();
            var engine = new FakeEngine();
            fetCollection.AddFeature<IEngineFeature>(engine);
            fetCollection.AddFeature<ITimeStepsFeature>(engine);
            fetCollection.AddFeature<IPathMappingFeature>(engine);

            gen.Process(block);

            for (var i = 0; i < 64 * 10; i++)
            {
                Assert.Equal(retVal, block[i]);
            }

            gen.UseNormalInverse = true;
            gen.Process(block);
            retVal = Statistics.NormInv(retVal);
            for (var i = 0; i < 64 * 10; i++)
            {
                Assert.Equal(retVal, block[i]);
            }
        }

    }
}
