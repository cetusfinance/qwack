using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests
{
    public static class TestProviderHelper
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "futuresettings.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICurrencyProvider CurrencyProvider = new CurrenciesFromJson(CalendarProvider, JsonCurrencyPath);
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider FutureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath);
    }
}
