using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;
using Qwack.Options.VolSurfaces;
using Qwack.Models;
using Qwack.Core.Basic.Correlation;

namespace Qwack.Math.Tests.Options
{
    public class LocalCorrelationFacts
    {
        [Fact]
        public void ConstVol()
        {
            var constVolA = 0.32;
            var constVolB = 0.16;
            var originDate = new DateTime(2017, 02, 21);
            var impliedSurfaceA = new ConstantVolSurface(originDate, constVolA) { AssetId = "A" };
            var impliedSurfaceB = new ConstantVolSurface(originDate, constVolB) { AssetId = "B" };

            var model = new AssetFxModel(originDate, null);
            model.AddVolSurface("A", impliedSurfaceA);
            model.AddVolSurface("B", impliedSurfaceB);

            var timesteps = Enumerable.Range(1, 4).Select(x => x / 4.0).ToArray();
            var termCorrels = new[] { 0.9, 0.85, 0.8, 0.75 };
            var correlVector = new CorrelationTimeVector("A", "B", termCorrels, timesteps);

            model.CorrelationMatrix = correlVector;

            var lc = model.LocalCorrelationRaw(timesteps);
            var lcVec = lc.Select(x => x[0][0]).ToArray();
            for (var i = 0; i < termCorrels.Length; i++)
            {
                var expected = lcVec.Take(i + 1).Average();
                Assert.Equal(expected, termCorrels[i], 8);
            }
        }
    }
}
