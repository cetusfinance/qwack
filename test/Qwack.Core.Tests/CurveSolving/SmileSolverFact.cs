using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Calibrators;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Math.Utils;
using Qwack.Options;
using Qwack.Options.Calibrators;
using Qwack.Options.VolSurfaces;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class SmileSolverFact
    {
        bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void SolveSmileSimple()
        {
            var valDate = new DateTime(2018, 07, 28);
            var expDate = valDate.AddDays(365);
            var fwd = 1000;
            double[] strikes = { 0.25, 0.5, 0.75 };

            var atmConstraint = new ATMStraddleConstraint
            {
                ATMVolType = AtmVolType.ZeroDeltaStraddle,
                MarketVol = 0.32
            };

            var smile25d = new RRBFConstraint
            {
                Delta = 0.25,
                FlyVol = 0.01,
                RisykVol = 0.02,
                WingQuoteType = WingQuoteType.Arithmatic
            };

            var s = new AssetSmileSolver();
            if (IsCoverageOnly)
                s.Tollerance = 1;

            var smile = s.Solve(atmConstraint, new[] { smile25d }, valDate, expDate, fwd, strikes, Interpolator1DType.Linear);

            if (!IsCoverageOnly)
            {
                Assert.Equal(atmConstraint.MarketVol, smile[1], 8);
                Assert.Equal(smile25d.RisykVol, smile[2] - smile[0], 8);
                Assert.Equal(smile25d.FlyVol, (smile[2] + smile[0]) / 2 - atmConstraint.MarketVol, 8);
            }
        }

        [Fact]
        public void SolveSmileMarket()
        {
            var valDate = new DateTime(2018, 07, 28);
            var expDate = valDate.AddDays(365);
            var tExp = (expDate - valDate).TotalDays / 365.0;
            var fwd = 1000;
            double[] strikes = { 0.25, 0.5, 0.75 };

            var atmConstraint = new ATMStraddleConstraint
            {
                ATMVolType = AtmVolType.ZeroDeltaStraddle,
                MarketVol = 0.32
            };

            var smile25d = new RRBFConstraint
            {
                Delta = 0.25,
                FlyVol = 0.01,
                RisykVol = 0.02,
                WingQuoteType = WingQuoteType.Market
            };

            var s = new AssetSmileSolver();
            if (IsCoverageOnly)
                s.Tollerance = 1;

            var smile = s.Solve(atmConstraint, new[] { smile25d }, valDate, expDate, fwd, strikes, Interpolator1DType.Linear);

            if (!IsCoverageOnly)
                Assert.Equal(atmConstraint.MarketVol, smile[1], 8);

            var surface = new GridVolSurface(valDate, strikes, new[] { expDate }, new[] { smile }, StrikeType.ForwardDelta, Interpolator1DType.Linear, Interpolator1DType.Linear, DayCountBasis.Act365F);

            //reprice market RR structrure off smile, premium must match
            var marketVolC = atmConstraint.MarketVol + smile25d.FlyVol + 0.5 * smile25d.RisykVol;
            var marketVolP = atmConstraint.MarketVol + smile25d.FlyVol - 0.5 * smile25d.RisykVol;
            var marketKC25 = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.25, 0, tExp, marketVolC);
            var marketKP25 = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.25, 0, tExp, marketVolP);
            var marketC25FV = BlackFunctions.BlackPV(fwd, marketKC25, 0, tExp, marketVolC, OptionType.C);
            var marketP25FV = BlackFunctions.BlackPV(fwd, marketKP25, 0, tExp, marketVolP, OptionType.P);
            var marketRR = marketC25FV - marketP25FV;

            var volC25d = surface.GetVolForAbsoluteStrike(marketKC25, expDate, fwd);
            var volP25d = surface.GetVolForAbsoluteStrike(marketKP25, expDate, fwd);
            var call25FV = BlackFunctions.BlackPV(fwd, marketKC25, 0, tExp, volC25d, OptionType.C);
            var put25FV = BlackFunctions.BlackPV(fwd, marketKP25, 0, tExp, volP25d, OptionType.P);
            var smileRR = call25FV - put25FV;

            if (!IsCoverageOnly)
                Assert.Equal(marketRR, smileRR, 8);

            //reprice market BF structrure off smile, premium must match
            var marketVolBF = atmConstraint.MarketVol + smile25d.FlyVol;
            var marketKBFC25 = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, 0.25, 0, tExp, marketVolBF);
            var marketKBFP25 = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, -0.25, 0, tExp, marketVolBF);
            var marketBFC25FV = BlackFunctions.BlackPV(fwd, marketKBFC25, 0, tExp, marketVolBF, OptionType.C);
            var marketBFP25FV = BlackFunctions.BlackPV(fwd, marketKBFP25, 0, tExp, marketVolBF, OptionType.P);
            var marketBF = marketBFC25FV + marketBFP25FV;

            var volCBF25d = surface.GetVolForAbsoluteStrike(marketKBFC25, expDate, fwd);
            var volPBF25d = surface.GetVolForAbsoluteStrike(marketKBFP25, expDate, fwd);
            var callBF25FV = BlackFunctions.BlackPV(fwd, marketKBFC25, 0, tExp, volCBF25d, OptionType.C);
            var putBF25FV = BlackFunctions.BlackPV(fwd, marketKBFP25, 0, tExp, volPBF25d, OptionType.P);
            var smileBF = callBF25FV + putBF25FV;

            if (!IsCoverageOnly)
                Assert.Equal(marketBF, smileBF, 8);

        }
    }
}
