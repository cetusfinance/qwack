using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using Qwack.Models;
using Qwack.Dates;
using static System.Math;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Core.Tests.Instruments
{
    public class FICFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void FundingInstrumentCollection()
        {
            var f = new FundingInstrumentCollection();

            f.Add(new FxForward { SolveCurve = "1.blah" });
            f.Add(new FxForward { SolveCurve = "1.blah" });
            f.Add(new FxForward { SolveCurve = "2.blah" });

            Assert.True(Enumerable.SequenceEqual(f.SolveCurves, new[] { "1.blah", "2.blah" }));
        }

        [Fact]
        public void PillarDatesTest()
        {
            var f = new FundingInstrumentCollection();

            f.Add(new FxForward { SolveCurve = "1.blah", PillarDate = DateTime.Today });
            f.Add(new FxForward { SolveCurve = "1.blah", PillarDate = DateTime.Today.AddDays(1) });
            f.Add(new FxForward { SolveCurve = "2.blah", PillarDate = DateTime.Today });

            var x = f.ImplyContainedCurves(DateTime.Today, Interpolator1DType.Linear);

            //double up on one pillar on the same curve
            f.Add(new FxForward { SolveCurve = "2.blah", PillarDate = DateTime.Today });

            Assert.Throws<Exception>(()=>f.ImplyContainedCurves(DateTime.Today, Interpolator1DType.Linear));
        }
    }
}