using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options.Asians;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Options.VolSurfaces;
using Qwack.Dates;

namespace Qwack.Math.Tests.Options
{
    public class ClewlowFacts
    {
        [Fact]
        public void PVFacts()
        {
            var evalDate = DateTime.Today;
            var avgStart = evalDate.AddDays(365);
            var avgEnd = avgStart.AddDays(32);
            var t = (avgEnd - evalDate).TotalDays / 365.0;

            var fixCal = new Calendar();

            var k = 0;
            var f = 100;
            var vol = 0.32;
            var rf = 0.05;

            //zero strike put is worthless
            var PV = LME_Clewlow.PV(f, 0, vol, 0.0, evalDate, avgStart, avgEnd, rf, OptionType.P, fixCal);
            Assert.Equal(0, PV, 10);

            //zero strike call is worth discounted fwd
            PV = LME_Clewlow.PV(f, 0, vol, 0.0, evalDate, avgStart, avgEnd, rf, OptionType.C, fixCal);
            Assert.Equal(System.Math.Exp(-rf*t)*f, PV, 2);

            //OTM option with zero vol is worthless
            vol = 0.0;
            k = f + 1;
            PV = LME_Clewlow.PV(f, 0, vol, 0.0, evalDate, avgStart, avgEnd, rf, OptionType.C, fixCal);
            Assert.Equal(0, PV, 10);

            //put-call parity at f==k
            k = f;
            vol = 0.32;
            var PVcall = LME_Clewlow.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, fixCal);
            var PVput = LME_Clewlow.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, fixCal);
            Assert.Equal(PVcall, PVput, 2);
        }

     

    }
}
