using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options.Asians;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Options.VolSurfaces;

namespace Qwack.Math.Tests.Options
{
    public class TurnbullWakemanFacts
    {
        [Fact]
        public void PVFacts()
        {
            var t = 1.0;
            var t2 = 2.0;
            var k = 0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;
            var cp = OptionType.P;
            //zero strike put is worthless
            var PV = TurnbullWakeman.PV(f, 0, vol, 0, t, t2, rf, cp);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth discounted fwd
            cp = OptionType.C;
            PV = TurnbullWakeman.PV(f, 0, vol, 0, t, t2, rf, cp);
            Assert.Equal(System.Math.Exp(-rf*t2)*f, PV, 2);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = TurnbullWakeman.PV(f, k, vol, 0, t, t2, rf, cp);
            Assert.Equal(0, PV, 10);

            //put-call parity at f==k
            k = f;
            vol = 0.32;
            var PVcall = TurnbullWakeman.PV(f, 0, vol, k, t, t2, rf, OptionType.C);
            var PVput = TurnbullWakeman.PV(f, 0, vol, k, t, t2, rf, OptionType.P);
            Assert.Equal(PVcall, PVput, 2);
        }

        [Fact]
        public void StrikeForPVFacts()
        {
            var evalDate = DateTime.Today;
            var avgStart = evalDate.AddDays(365);
            var avgEnd = avgStart.AddDays(32);
            var k = 110;
            var f = 100;
            var vol = 0.32;
            var rf = 0.0;

            var volSurface = new ConstantVolSurface(evalDate, vol);


            var pv = TurnbullWakeman.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);

            var strike = TurnbullWakeman.StrikeForPV(pv, f, 0, volSurface, DateTime.Today, avgStart, avgEnd, rf, OptionType.C);
            Assert.Equal(k, strike, 10);
        }

    }
}
