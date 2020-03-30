using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qwack.Dates;
using Qwack.Providers.Json;
using Qwack.Utils;

namespace Qwack.CLI
{
    public static class ContainerStores
    {
        private const string _calendarJSONFile = "Calendars.json";

        static ContainerStores()
        {
                GlobalContainer = ((IServiceCollection)new ServiceCollection())
                    .AddQwackLogging()
                    .AddCalendarsFromJson(GetCalendarFilename())
                    .BuildServiceProvider();

            _sessionContainer = GlobalContainer.CreateScope().ServiceProvider;
        }

        public static IServiceProvider GlobalContainer { get; internal set; }

        private static IServiceProvider _sessionContainer;

        public static ICalendarProvider CalendarProvider => GlobalContainer.GetRequiredService<ICalendarProvider>();

        public static ILogger GetLogger<T>() => GlobalContainer.GetRequiredService<ILoggerFactory>().CreateLogger<T>();

        private static string GetRunningDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return dirPath;
        }

        private static string GetCalendarFilename() => Path.Combine(GetRunningDirectory(), _calendarJSONFile);
    }
}
