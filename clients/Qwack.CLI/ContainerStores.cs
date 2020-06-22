using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.Json;
using Qwack.Utils;

namespace Qwack.CLI
{
    public static class ContainerStores
    {
        private const string _calendarJSONFile = "Calendars.json";
        private const string _futureSettingsFile = "futuresettings.json";
        private const string _currenciesFile = "currencies.json";
        static ContainerStores()
        {
                GlobalContainer = ((IServiceCollection)new ServiceCollection())
                    .AddQwackLogging()
                    .AddCalendarsFromJson(GetCalendarFilename())
                    .AddFutureSettingsFromJson(GetFutureSettingsFile())
                    .AddCurrenciesFromJson(GetCurrenciesFilename())
                    .BuildServiceProvider();

            _sessionContainer = GlobalContainer.CreateScope().ServiceProvider;
        }

        public static IServiceProvider GlobalContainer { get; internal set; }

        private static IServiceProvider _sessionContainer;

        public static ICurrencyProvider CurrencyProvider => GlobalContainer.GetRequiredService<ICurrencyProvider>();
        public static ICalendarProvider CalendarProvider => GlobalContainer.GetRequiredService<ICalendarProvider>();
        public static IFutureSettingsProvider FuturesProvider => GlobalContainer.GetRequiredService<IFutureSettingsProvider>();

        public static ILogger GetLogger<T>() => GlobalContainer.GetRequiredService<ILoggerFactory>().CreateLogger<T>();

        private static string GetRunningDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return dirPath;
        }

        private static string GetCalendarFilename() => Path.Combine(GetRunningDirectory(), _calendarJSONFile);
        private static string GetFutureSettingsFile() => Path.Combine(GetRunningDirectory(), _futureSettingsFile);
        private static string GetCurrenciesFilename() => Path.Combine(GetRunningDirectory(), _currenciesFile);
    }
}
