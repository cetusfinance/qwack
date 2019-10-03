using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.Json;

namespace Qwack.MonteCarlo.Test
{
    public static class TestProviderHelper
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ILoggerFactory LoggerFactory = new LoggerFactory();
        public static readonly ICurrencyProvider CurrencyProvider = new CurrenciesFromJson(CalendarProvider, JsonCurrencyPath, LoggerFactory);
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider FutureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath, LoggerFactory);
    }
}
