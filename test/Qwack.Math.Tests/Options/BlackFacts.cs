using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;

namespace Qwack.Math.Tests.Options
{
    public class BlackFacts
    {
        [Fact]
        public void PVFacts()
        {
            var t = 1.0;
            var k = 0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = "P";

            //zero strike put is worthless
            var PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth discounted fwd
            cp = "C";
            PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(System.Math.Exp(-rf*t)*f, PV, 10);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = BlackFunctions.BlackPV(f, k, rf, t, vol, cp);
            Assert.Equal(0, PV, 10);

            //put-call parity at f==k
            k = f;
            vol = 0.32;
            var PVcall = BlackFunctions.BlackPV(f, k, rf, t, vol, "C");
            var PVput = BlackFunctions.BlackPV(f, k, rf, t, vol, "P");
            Assert.Equal(PVcall, PVput, 10);

            //forward price constant wrt total variance
            rf = 0;
            var variance = vol * vol * t;
            var t2 = t * 2.0;
            var vol2 = System.Math.Sqrt(variance / t2);

            var PVnear = BlackFunctions.BlackPV(f, k, rf, t, vol, "C");
            var PVfar = BlackFunctions.BlackPV(f, k, rf, t2, vol2, "C");
            Assert.Equal(PVnear, PVfar, 10);
        }

        [Fact]
        public void VegaFacts()
        {
            var t = 1.0;
            var k = 100;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = "P";

            //vega closely matches numerical estimate
            var PV1 = BlackFunctions.BlackPV(f, k, rf, t, vol-0.00005, cp);
            var PV2 = BlackFunctions.BlackPV(f, k, rf, t, vol+0.00005, cp);
            var vegaEst = (PV2 - PV1) / 0.0001 * 0.01;
            var vega = BlackFunctions.BlackVega(f, k, rf, t, vol);
            Assert.Equal(vegaEst, vega, 6);

            //all else the same, more time==more vega
            var vegaNear = BlackFunctions.BlackVega(f, k, rf, t, vol);
            var vegaFar = BlackFunctions.BlackVega(f, k, rf, t*2, vol);
            Assert.True(vegaFar > vegaNear);

            //cases for zero vega
            vega = BlackFunctions.BlackVega(f, 0, rf, t, vol);
            Assert.Equal(0, vega, 8);

            vega = BlackFunctions.BlackVega(f, 1e6, rf, t, vol);
            Assert.Equal(0, vega, 8);
        }
    }
}