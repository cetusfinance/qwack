using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options.Asians;
using Xunit;
using Qwack.Core.Basic;
using Qwack.Options.VolSurfaces;
using Qwack.Dates;
using Qwack.Options;

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

            //expired is worthless
            var PV = TurnbullWakeman.PV(f, 0, vol, 0, t, 0, rf, OptionType.P);
            Assert.Equal(0, PV, 10);
            PV = TurnbullWakeman.PV(f, 0, vol, 0, t, 0, rf, OptionType.C);
            Assert.Equal(0, PV, 10);

            //zero strike put is worthless
            PV = TurnbullWakeman.PV(f, 0, vol, 0, t, t2, rf, cp);
            Assert.Equal(0, PV, 10);
            PV = TurnbullWakeman.PV(f, 50, vol, 0, -0.1, t2, rf, cp);
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

            //independent fwds version
            var valDate = new DateTime(2019, 10, 24);
            var fixingStartDate = new DateTime(2019, 10, 01);
            var fixingEndDate = new DateTime(2019, 10, 25);
            var fixingDates = DateExtensions.BusinessDaysInPeriod(fixingStartDate, fixingEndDate, TestProviderHelper.CalendarProvider.Collection["NYC"]).ToArray();
            var fwds = fixingDates.Select(x => 100.0).ToArray();
            var sigmas = fixingDates.Select(x => 0.32).ToArray();
            PV = TurnbullWakeman.PV(fwds, fixingDates, valDate, fixingEndDate, sigmas, 1, 0.0, OptionType.C, true);
            var blackPV = BlackFunctions.BlackPV(100.0, 1, 0.0, 1 / 365.0, 0.32, OptionType.C);
            Assert.Equal(blackPV, PV, 4);
        }

        [Fact]
        public void StrikeForPVFacts()
        {
            var evalDate = DateTime.Today;
            var avgStart = evalDate.AddDays(365);
            var avgEnd = avgStart.AddDays(32);
            var k = 110.0;
            var f = 100.0;
            var vol = 0.32;
            var rf = 0.0;

            var volSurface = new ConstantVolSurface(evalDate, vol);


            var pv = TurnbullWakeman.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);

            var strike = TurnbullWakeman.StrikeForPV(pv, f, 0, volSurface, DateTime.Today, avgStart, avgEnd, rf, OptionType.C);
            Assert.Equal(k, strike, 10);

            var fixingDates = avgStart.CalendarDaysInPeriod(avgEnd).ToArray();
            var fwds = fixingDates.Select(d => f).ToArray();
            var sigmas = fixingDates.Select(d => vol).ToArray();
            pv = TurnbullWakeman.PV(fwds, fixingDates, evalDate, avgEnd,sigmas, k,rf, OptionType.C);
            strike = TurnbullWakeman.StrikeForPV(pv,fwds ,fixingDates, volSurface, evalDate, avgEnd, rf, OptionType.C);
            Assert.Equal(k, strike, 10);
        }

        [Fact]
        public void ThetaFacts()
        {
            var originDate = new DateTime(2019, 05, 10);
            var fixingDates = originDate.AddDays(30).CalendarDaysInPeriod(originDate.AddDays(60)).ToArray();
            var fwds = fixingDates.Select(d => 100.0).ToArray();
            var vols = fixingDates.Select(d => 0.32).ToArray();
            var k = 100.0;
            var pv = TurnbullWakeman.PV(fwds, fixingDates, originDate, fixingDates.Last(), vols, k, 0.0, OptionType.C);
            var pv2 = TurnbullWakeman.PV(fwds, fixingDates, originDate.AddDays(1), fixingDates.Last(), vols, k, 0.0, OptionType.C);
            var theta = TurnbullWakeman.Theta(fwds, fixingDates, originDate, fixingDates.Last(), vols, k, 0.0, OptionType.C);

            Assert.Equal((pv2 - pv) * 365, theta, 0);

            theta = TurnbullWakeman.Theta(fwds, fixingDates, fixingDates.Last().AddDays(1), fixingDates.Last(), vols, k, 0.0, OptionType.C);
            Assert.Equal(0, theta);

            theta = TurnbullWakeman.Theta(fwds, fixingDates, fixingDates.Last(), fixingDates.Last(), vols, k, 0.0, OptionType.C);
            Assert.Equal(0, theta);

            Assert.Throws<DataMisalignedException>(() => TurnbullWakeman.Theta(new[] { 1.0 }, fixingDates, originDate, fixingDates.Last(), vols, k, 0.0, OptionType.C));
        }

        [Fact]
        public void DeltaFacts()
        {
            var evalDate = new DateTime(2019, 05, 10);
            var avgStart = evalDate.AddDays(365);
            var avgEnd = avgStart.AddDays(32);
            var k = 100.0;
            var f = 100.0;
            var vol = 0.32;
            var rf = 0.0;
            var deltaBump = 1e-6;
            var pv = TurnbullWakeman.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            var pv2 = TurnbullWakeman.PV(f + deltaBump, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            var delta = TurnbullWakeman.Delta(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);

            Assert.Equal((pv2 - pv)/ deltaBump, delta, 2);

            evalDate = avgStart.AddDays(16);
            pv = TurnbullWakeman.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            pv2 = TurnbullWakeman.PV(f + deltaBump, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            delta = TurnbullWakeman.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = TurnbullWakeman.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);
            pv2 = TurnbullWakeman.PV(f + deltaBump, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);
            delta = TurnbullWakeman.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = TurnbullWakeman.PV(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            pv2 = TurnbullWakeman.PV(f + deltaBump, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);
            delta = TurnbullWakeman.Delta(f, 0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.C);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);

            pv = TurnbullWakeman.PV(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);
            pv2 = TurnbullWakeman.PV(f + deltaBump, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);
            delta = TurnbullWakeman.Delta(f, 100.0, vol, k, evalDate, avgStart, avgEnd, rf, OptionType.P);

            Assert.Equal((pv2 - pv) / deltaBump, delta, 2);
        }

    }
}
