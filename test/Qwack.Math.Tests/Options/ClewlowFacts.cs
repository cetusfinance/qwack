using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options.Asians;
using Xunit;
using Qwack.Options.VolSurfaces;
using Qwack.Dates;
using Qwack.Options;
using Qwack.Transport.BasicTypes;

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

            //bullet defaults to european
            avgStart = avgEnd;
            PV = LME_Clewlow.PV(f, 0.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, fixCal);
            var blackPV = BlackFunctions.BlackPV(f, k, rf, t, vol, OptionType.P);
            Assert.Equal(blackPV, PV, 10);

            //on expiry its intrinsic
            evalDate = avgEnd;
            PV = LME_Clewlow.PV(f, f, vol, f+10, evalDate, avgStart, avgEnd, rf, OptionType.P, fixCal);
            Assert.Equal(10.0, PV, 10);
            PV = LME_Clewlow.PV(f,f, vol, f - 10, evalDate, avgStart, avgEnd, rf, OptionType.C, fixCal);
            Assert.Equal(10.0, PV, 10);

        }

        [Fact]
        public void DeltaFacts()
        {
            var cal = TestProviderHelper.CalendarProvider.Collection["nyc"];

            var evalDate = new DateTime(2019, 05, 10);
            var avgStart = evalDate.AddDays(365);
            var avgEnd = avgStart.AddDays(32);
            var k = 100.0;
            var f = 100.0;
            var vol = 0.32;
            var rf = 0.0;
            var deltaBump = 1e-6;
            var pv = LME_Clewlow.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            var pv2 = LME_Clewlow.PV(f + deltaBump, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            var delta = LME_Clewlow.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            evalDate = avgStart.AddDays(16);
            pv = LME_Clewlow.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            pv2 = LME_Clewlow.PV(f + deltaBump, f+deltaBump, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            delta = LME_Clewlow.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = LME_Clewlow.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);
            pv2 = LME_Clewlow.PV(f + deltaBump, f + deltaBump, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);
            delta = LME_Clewlow.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = LME_Clewlow.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            pv2 = LME_Clewlow.PV(f + deltaBump, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);
            delta = LME_Clewlow.Delta(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C, cal);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = LME_Clewlow.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);
            pv2 = LME_Clewlow.PV(f + deltaBump, f + deltaBump, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);
            delta = LME_Clewlow.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P, cal);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);
        }

    }
}
