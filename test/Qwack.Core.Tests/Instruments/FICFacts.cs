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
using Qwack.Providers.Json;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Tests.Instruments
{
    public class FICFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void FundingInstrumentCollection()
        {
            var f = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                new FxForward { SolveCurve = "1.blah" },
                new FxForward { SolveCurve = "1.blah" },
                new FxForward { SolveCurve = "2.blah" }
            };

            Assert.True(Enumerable.SequenceEqual(f.SolveCurves, new[] { "1.blah", "2.blah" }));

            var f2 = f.Clone();
            Assert.True(Enumerable.SequenceEqual(f, f2));
        }

        [Fact]
        public void PillarDatesTest()
        {
            var f = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                new FxForward { SolveCurve = "usd.1blah", PillarDate = DateTime.Today },
                new FxForward { SolveCurve = "usd.2blah", PillarDate = DateTime.Today.AddDays(1) },
                new FxForward { SolveCurve = "usd.2blah", PillarDate = DateTime.Today }
            };

            var x = f.ImplyContainedCurves(DateTime.Today, Interpolator1DType.Linear);

            //double up on one pillar on the same curve
            f.Add(new FxForward { SolveCurve = "usd.2blah", PillarDate = DateTime.Today });

            Assert.Throws<Exception>(()=>f.ImplyContainedCurves(DateTime.Today, Interpolator1DType.Linear));
        }

        [Fact]
        public void ImplySolveStagesTest()
        {
            var f = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                new IrSwap
                {
                    SolveCurve = "usd.forecast",
                    DiscountCurve = "usd.discount",
                    ForecastCurve = "usd.forecast"
                },
                new IrBasisSwap
                {
                    SolveCurve = "usd.discount",
                    DiscountCurve = "usd.discount",
                    ForecastCurvePay = "usd.forecast",
                    ForecastCurveRec = "usd.discount"
                },
                new XccyBasisSwap
                {
                    SolveCurve = "zar.discount.usd",
                    DiscountCurvePay = "zar.discount.usd",
                    ForecastCurvePay ="zar.discount.usd",
                    DiscountCurveRec ="usd.discount",
                    ForecastCurveRec ="usd.forecast"
                }
            };

            var x = f.ImplySolveStages(null);

            Assert.Equal(3, x.Count);
            Assert.Equal(2, x.Where(xx => xx.Value == 0).Count());
            Assert.Equal("zar.discount.usd", x.Single(xx => xx.Value == 1).Key);

            f.Add(new IrBasisSwap
            {
                SolveCurve = "usd.discount2",
                DiscountCurve = "usd.discount2",
                ForecastCurvePay = "usd.forecast2",
                ForecastCurveRec = "usd.discount2"
            });

            Assert.Throws<Exception>(() => f.ImplySolveStages(null));
        }
    }
}
