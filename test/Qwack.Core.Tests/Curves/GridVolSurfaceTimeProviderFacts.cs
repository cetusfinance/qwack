using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Curves.TimeProviders;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Curves
{
    public class GridVolSurfaceTimeProviderFacts
    {
        private static Calendar CreateWeekdayCalendar(params DateTime[] holidays)
        {
            var cal = new Calendar
            {
                Name = "Test",
                DaysToAlwaysExclude = new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday },
                DaysToExclude = new HashSet<DateTime>(holidays),
            };
            return cal;
        }

        private static GridVolSurface BuildSurface(DateTime origin, DateTime[] expiries, double[] strikes, double[][] vols, ITimeProvider timeProvider)
        {
            var surface = new GridVolSurface
            {
                StrikeType = StrikeType.Absolute,
                StrikeInterpolatorType = Interpolator1DType.LinearFlatExtrap,
                TimeInterpolatorType = Interpolator1DType.LinearInVariance,
                TimeBasis = DayCountBasis.Act365F,
                TimeProvider = timeProvider,
            };
            surface.Build(origin, strikes, expiries, vols);
            return surface;
        }

        /// <summary>
        /// Both CalendarTimeProvider and BusinessDayTimeProvider should return the exact
        /// input volatility when queried at a pillar date (no interpolation involved).
        /// </summary>
        [Fact]
        public void CalendarTimeProvider_ReturnsPillarVols()
        {
            var origin = new DateTime(2024, 1, 2); // Tuesday
            var expiries = new[]
            {
                new DateTime(2024, 4, 2),
                new DateTime(2024, 7, 2),
                new DateTime(2025, 1, 2),
            };
            var strikes = new[] { 90.0, 100.0, 110.0 };
            var vols = new[]
            {
                new[] { 0.28, 0.25, 0.27 },
                new[] { 0.26, 0.23, 0.25 },
                new[] { 0.24, 0.21, 0.23 },
            };

            var calProvider = new CalendarTimeProvider();
            var surface = BuildSurface(origin, expiries, strikes, vols, calProvider);

            for (var i = 0; i < expiries.Length; i++)
            {
                for (var j = 0; j < strikes.Length; j++)
                {
                    var vol = surface.GetVolForAbsoluteStrike(strikes[j], expiries[i], 100.0);
                    Assert.Equal(vols[i][j], vol, 10);
                }
            }
        }

        [Fact]
        public void BusinessDayTimeProvider_ReturnsPillarVols()
        {
            var origin = new DateTime(2024, 1, 2);
            var expiries = new[]
            {
                new DateTime(2024, 4, 2),
                new DateTime(2024, 7, 2),
                new DateTime(2025, 1, 2),
            };
            var strikes = new[] { 90.0, 100.0, 110.0 };
            var vols = new[]
            {
                new[] { 0.28, 0.25, 0.27 },
                new[] { 0.26, 0.23, 0.25 },
                new[] { 0.24, 0.21, 0.23 },
            };

            var cal = CreateWeekdayCalendar();
            var bdProvider = new BusinessDayTimeProvider(cal);
            var surface = BuildSurface(origin, expiries, strikes, vols, bdProvider);

            for (var i = 0; i < expiries.Length; i++)
            {
                for (var j = 0; j < strikes.Length; j++)
                {
                    var vol = surface.GetVolForAbsoluteStrike(strikes[j], expiries[i], 100.0);
                    Assert.Equal(vols[i][j], vol, 10);
                }
            }
        }

        /// <summary>
        /// The two time providers should yield different year fractions between pillars,
        /// and therefore different interpolated volatilities at intermediate dates.
        /// The BusinessDayTimeProvider with zero weekend weight compresses time, so
        /// the year fraction to a mid-point should be proportionally smaller, pushing
        /// the interpolated vol closer to the nearer pillar.
        /// </summary>
        [Fact]
        public void CalendarVsBusinessDay_DifferBetweenPillars()
        {
            var origin = new DateTime(2024, 1, 2);
            var expiries = new[]
            {
                new DateTime(2024, 4, 2),
                new DateTime(2025, 1, 2),
            };
            var strikes = new[] { 100.0 };
            var vols = new[]
            {
                new[] { 0.30 },
                new[] { 0.20 },
            };

            var calProvider = new CalendarTimeProvider();
            var calSurface = BuildSurface(origin, expiries, strikes, vols, calProvider);

            var cal = CreateWeekdayCalendar();
            var bdProvider = new BusinessDayTimeProvider(cal);
            var bdSurface = BuildSurface(origin, expiries, strikes, vols, bdProvider);

            // Pick a date between the two pillars
            var midDate = new DateTime(2024, 7, 15); // Monday
            var calVol = calSurface.GetVolForAbsoluteStrike(100.0, midDate, 100.0);
            var bdVol = bdSurface.GetVolForAbsoluteStrike(100.0, midDate, 100.0);

            // Both should be between the pillar vols
            Assert.True(calVol > 0.20 && calVol < 0.30,
                $"Calendar vol {calVol:F6} should be between pillar vols");
            Assert.True(bdVol > 0.20 && bdVol < 0.30,
                $"Business day vol {bdVol:F6} should be between pillar vols");

            // They should differ because the year fractions are different
            Assert.NotEqual(calVol, bdVol, 4);
        }

        /// <summary>
        /// With zero weekend weight, weekends don't contribute to time.
        /// Extending the end date across weekend days should not change the year fraction,
        /// and the result should be less than CalendarTimeProvider (which counts all days).
        /// </summary>
        [Fact]
        public void BusinessDayProvider_ZeroWeekendWeight_ExcludesWeekends()
        {
            var cal = CreateWeekdayCalendar();
            var providerZero = new BusinessDayTimeProvider(cal, weekendWeight: 0.0);
            var calProvider = new CalendarTimeProvider();

            var monday = new DateTime(2023, 1, 9);
            var saturday = new DateTime(2023, 1, 14);
            var sunday = new DateTime(2023, 1, 15);
            var nextMonday = new DateTime(2023, 1, 16);

            // With zero weight, extending across the weekend should not change
            // the year fraction (weekends contribute nothing)
            var yfSat = providerZero.GetYearFraction(monday, saturday);
            var yfSun = providerZero.GetYearFraction(monday, sunday);
            var yfNextMon = providerZero.GetYearFraction(monday, nextMonday);

            Assert.Equal(yfSat, yfSun, 10);
            Assert.Equal(yfSat, yfNextMon, 10);

            // BD with zero weekend weight should give less time than CalendarTimeProvider
            var yfCal = calProvider.GetYearFraction(monday, nextMonday);
            Assert.True(yfNextMon < yfCal,
                $"BD zero-weight ({yfNextMon:F6}) should be less than Calendar ({yfCal:F6})");
        }

        /// <summary>
        /// With weekend weight = 1.0, all days count equally.
        /// For a non-leap year, the result should exactly match CalendarTimeProvider (Act/365F).
        /// </summary>
        [Fact]
        public void BusinessDayProvider_FullWeekendWeight_MatchesCalendar()
        {
            var cal = CreateWeekdayCalendar();
            var bdProvider = new BusinessDayTimeProvider(cal, weekendWeight: 1.0, holidayWeight: 1.0);
            var calProvider = new CalendarTimeProvider();

            // Exactly one year should be 1.0 for both providers
            var start = new DateTime(2023, 1, 2);
            var oneYear = new DateTime(2024, 1, 2);
            Assert.Equal(1.0, bdProvider.GetYearFraction(start, oneYear), 10);
            Assert.Equal(1.0, calProvider.GetYearFraction(start, oneYear), 10);

            // For sub-year periods in a non-leap year, BD with full weights
            // should exactly match CalendarTimeProvider (both count all days / 365)
            var end = new DateTime(2023, 7, 15);
            var yfBd = bdProvider.GetYearFraction(start, end);
            var yfCal = calProvider.GetYearFraction(start, end);

            Assert.Equal(yfCal, yfBd, 10);
        }

        /// <summary>
        /// With weekend weight = 0.5, weekends count as half-days.
        /// The year fraction should differ from both the zero-weight and full-weight cases.
        /// Note: because the year fraction is a ratio (weighted partial / weighted full year),
        /// the ordering depends on the weekend distribution of the specific dates.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_HalfWeekendWeight_IsBetween()
        {
            var cal = CreateWeekdayCalendar();
            var bdZero = new BusinessDayTimeProvider(cal, weekendWeight: 0.0);
            var bdHalf = new BusinessDayTimeProvider(cal, weekendWeight: 0.5);
            var bdFull = new BusinessDayTimeProvider(cal, weekendWeight: 1.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2024, 7, 15);

            var yfZero = bdZero.GetYearFraction(start, end);
            var yfHalf = bdHalf.GetYearFraction(start, end);
            var yfFull = bdFull.GetYearFraction(start, end);

            // Half weight should be distinct from both zero and full
            Assert.NotEqual(yfZero, yfHalf, 6);
            Assert.NotEqual(yfHalf, yfFull, 6);

            // All should be positive and in a sensible range for ~6 months
            Assert.True(yfZero > 0.4 && yfZero < 0.6,
                $"Zero-weight ({yfZero:F6}) should be roughly half a year");
            Assert.True(yfHalf > 0.4 && yfHalf < 0.6,
                $"Half-weight ({yfHalf:F6}) should be roughly half a year");
            Assert.True(yfFull > 0.4 && yfFull < 0.6,
                $"Full-weight ({yfFull:F6}) should be roughly half a year");
        }

        /// <summary>
        /// Holidays with zero holiday weight should be excluded from time,
        /// giving a smaller year fraction than without the holiday.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_ZeroHolidayWeight_ExcludesHolidays()
        {
            // Add some holidays on weekdays
            var holidays = new[]
            {
                new DateTime(2024, 3, 29), // Good Friday
                new DateTime(2024, 4, 1),  // Easter Monday
                new DateTime(2024, 5, 6),  // Bank holiday Monday
            };

            var calNoHols = CreateWeekdayCalendar();
            var calWithHols = CreateWeekdayCalendar(holidays);

            var providerNoHols = new BusinessDayTimeProvider(calNoHols, weekendWeight: 0.0, holidayWeight: 0.0);
            var providerWithHols = new BusinessDayTimeProvider(calWithHols, weekendWeight: 0.0, holidayWeight: 0.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2024, 6, 28);

            var yfNoHols = providerNoHols.GetYearFraction(start, end);
            var yfWithHols = providerWithHols.GetYearFraction(start, end);

            // With holidays excluded, the year fraction should be smaller
            Assert.True(yfWithHols < yfNoHols,
                $"With holidays excluded ({yfWithHols:F6}) should be less than without ({yfNoHols:F6})");
        }

        /// <summary>
        /// Holidays with weight = 1.0 should be treated as normal days,
        /// so the result should match having no holidays at all.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_FullHolidayWeight_TreatsHolidaysAsNormal()
        {
            var holidays = new[]
            {
                new DateTime(2024, 3, 29),
                new DateTime(2024, 4, 1),
            };

            var calNoHols = CreateWeekdayCalendar();
            var calWithHols = CreateWeekdayCalendar(holidays);

            var providerNoHols = new BusinessDayTimeProvider(calNoHols, weekendWeight: 0.0, holidayWeight: 1.0);
            var providerWithHols = new BusinessDayTimeProvider(calWithHols, weekendWeight: 0.0, holidayWeight: 1.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2024, 6, 28);

            var yfNoHols = providerNoHols.GetYearFraction(start, end);
            var yfWithHols = providerWithHols.GetYearFraction(start, end);

            // With holiday weight = 1.0, holidays count as business days, so fractions should match
            Assert.Equal(yfNoHols, yfWithHols, 10);
        }

        /// <summary>
        /// Half holiday weight should give a year fraction between the zero and full holiday weight cases.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_HalfHolidayWeight_IsBetween()
        {
            var holidays = new[]
            {
                new DateTime(2024, 3, 29),
                new DateTime(2024, 4, 1),
                new DateTime(2024, 5, 6),
                new DateTime(2024, 5, 27),
            };

            var cal = CreateWeekdayCalendar(holidays);

            var providerZero = new BusinessDayTimeProvider(cal, weekendWeight: 0.0, holidayWeight: 0.0);
            var providerHalf = new BusinessDayTimeProvider(cal, weekendWeight: 0.0, holidayWeight: 0.5);
            var providerFull = new BusinessDayTimeProvider(cal, weekendWeight: 0.0, holidayWeight: 1.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2024, 6, 28);

            var yfZero = providerZero.GetYearFraction(start, end);
            var yfHalf = providerHalf.GetYearFraction(start, end);
            var yfFull = providerFull.GetYearFraction(start, end);

            Assert.True(yfZero < yfHalf,
                $"Zero holiday weight ({yfZero:F6}) should be less than half ({yfHalf:F6})");
            Assert.True(yfHalf < yfFull,
                $"Half holiday weight ({yfHalf:F6}) should be less than full ({yfFull:F6})");
        }

        /// <summary>
        /// BusinessDayTimeProvider should handle reversed dates (end before start)
        /// by returning a negative year fraction.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_ReversedDates_ReturnsNegative()
        {
            var cal = CreateWeekdayCalendar();
            var provider = new BusinessDayTimeProvider(cal, weekendWeight: 0.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2024, 7, 2);

            var yfForward = provider.GetYearFraction(start, end);
            var yfBackward = provider.GetYearFraction(end, start);

            Assert.True(yfForward > 0, "Forward year fraction should be positive");
            Assert.True(yfBackward < 0, "Backward year fraction should be negative");
            Assert.Equal(yfForward, -yfBackward, 10);
        }

        /// <summary>
        /// With zero weekend weight, a period of exactly one year should still return 1.0
        /// because the whole-year boundary is handled first.
        /// </summary>
        [Fact]
        public void BusinessDayProvider_ExactOneYear_ReturnsOne()
        {
            var cal = CreateWeekdayCalendar();
            var provider = new BusinessDayTimeProvider(cal, weekendWeight: 0.0);

            var start = new DateTime(2024, 1, 2);
            var end = new DateTime(2025, 1, 2);

            var yf = provider.GetYearFraction(start, end);
            Assert.Equal(1.0, yf, 10);
        }
    }
}
