using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Providers.Json;

namespace Qwack.Curves.Benchmark
{
    public static class CurveDataSetup
    {
        public static readonly string _jsonCalendarPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "data", "Calendars.json");
        public static readonly ICalendarProvider _calendarProvider = CalendarsFromJson.Load(_jsonCalendarPath);
        public static readonly Calendar _jhb = _calendarProvider.Collection["jhb"];
        public static readonly Currency ccyZar = new Currency("JHB", DayCountBasis.Act_365F, _jhb);
        public static readonly Calendar _usd = _calendarProvider.Collection["nyc"];
        public static readonly Currency ccyUsd = new Currency("USD", DayCountBasis.Act_360, _usd);
        public static readonly FloatRateIndex _zar3m = new FloatRateIndex()
        {
            Currency = ccyZar,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 3.Months(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = _jhb,
            RollConvention = RollType.MF
        };
        public static readonly FloatRateIndex zaron = new FloatRateIndex()
        {
            Currency = ccyZar,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = _jhb,
            RollConvention = RollType.F
        };
        public static readonly FloatRateIndex usdon = new FloatRateIndex()
        {
            Currency = ccyUsd,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 1.Years(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = _usd,
            RollConvention = RollType.F
        };
        public static readonly FloatRateIndex usd3m = new FloatRateIndex()
        {
            Currency = ccyUsd,
            DayCountBasis = DayCountBasis.Act_360,
            DayCountBasisFixed = DayCountBasis.Act_360,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 1.Years(),
            FixingOffset = 2.Bd(),
            HolidayCalendars = _usd,
            RollConvention = RollType.MF
        };
    }
}
