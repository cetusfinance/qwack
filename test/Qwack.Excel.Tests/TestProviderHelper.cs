using System;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests
{
    public static class TestProviderHelper
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Calendars.json");
        public static readonly string JsonFuturesPath = System.IO.Path.Combine(AppContext.BaseDirectory, "futuresettings.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Currencies.json");
        public static readonly ILoggerFactory LoggerFactory = new LoggerFactory();
        public static readonly ICurrencyProvider CurrencyProvider = new CurrenciesFromJson(CalendarProvider, JsonCurrencyPath, LoggerFactory.CreateLogger<CurrenciesFromJson>());
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly IFutureSettingsProvider FutureSettingsProvider = new FutureSettingsFromJson(CalendarProvider, JsonFuturesPath, LoggerFactory);
    }
}
