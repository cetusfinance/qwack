using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;

namespace Qwack.Math.Tests.Options.VolSurfaces
{
    public class ConstantVolSurfaceFacts
    {
        [Fact]
        public void ConstantVolSurface()
        {
            var origin = new DateTime(2017, 02, 07);
            var vol = 0.32;
            var surface = new Qwack.Options.VolSurfaces.ConstantVolSurface(origin,vol);

            Assert.Equal(vol, surface.GetVolForAbsoluteStrike(999, origin.AddDays(33),1), 12);
            Assert.Equal(vol, surface.GetVolForDeltaStrike(999, origin.AddDays(303),100), 12);
            Assert.Equal(vol, surface.GetVolForAbsoluteStrike(999, 0.777,0), 12);
            Assert.Equal(vol, surface.GetVolForDeltaStrike(999, 0.123,55), 12);
        }


    }
}