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
using Qwack.Excel.Utils;
using Qwack.Futures;
using Qwack.Models.Models;
using Qwack.Providers.Json;
using Qwack.Utils;

namespace Qwack.Excel
{
    public static class ContainerStores
    {
        private const string _calendarJSONFile = "Calendars.json";
        private const string _futureSettingsFile = "futuresettings.json";
        private const string _currenciesFile = "currencies.json";

        private static object _lock = new object();
        private static object _sessionLock = new object();

        static ContainerStores()
        {
            try
            {
                GlobalContainer = ((IServiceCollection)new ServiceCollection())
                    .AddQwackLogging()
                    .AddCalendarsFromJson(GetCalendarFilename())
                    .AddFutureSettingsFromJson(GetFutureSettingsFile())
                    .AddCurrenciesFromJson(GetCurrenciesFilename())
                    .AddSingleton(typeof(IObjectStore<>), typeof(ExcelObjectStore<>))
                    .BuildServiceProvider();

                SessionContainer = GlobalContainer.CreateScope().ServiceProvider;

                SessionContainer.GetRequiredService<IFutureSettingsProvider>();

                PnLAttributor = new PnLAttributor();
            }
            catch (Exception ex)
            {
                if (Directory.Exists(@"C:\Temp"))
                {
                    File.WriteAllText($@"C:\Temp\QwackInitializationError_{DateTime.Now:yyyyMMdd_HHmmss}.txt", ex.ToString());
                }
            }
        }

        public static IServiceProvider GlobalContainer { get; internal set; }

        private static IServiceProvider _sessionContainer;
        public static IServiceProvider SessionContainer
        {
            get
            {
                lock (_sessionLock)
                { return _sessionContainer; }
            }
            set
            {
                lock (_sessionLock)
                {
                    _sessionContainer = value;
                }
            }
        }
        public static ICurrencyProvider CurrencyProvider => GlobalContainer.GetRequiredService<ICurrencyProvider>();
        public static ICalendarProvider CalendarProvider => GlobalContainer.GetRequiredService<ICalendarProvider>();
        public static IFutureSettingsProvider FuturesProvider => GlobalContainer.GetRequiredService<IFutureSettingsProvider>();
        public static ILogger GetLogger<T>() => GlobalContainer.GetRequiredService<ILoggerFactory>().CreateLogger<T>();

        private static ConcurrentDictionary<Type, IFlushable> _registeredTypes = new ConcurrentDictionary<Type, IFlushable>();

        public static IPnLAttributor PnLAttributor { get; set; }

        private static string GetFutureSettingsFile() => Path.Combine(GetRunningDirectory(), _futureSettingsFile);

        private static string GetRunningDirectory()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return dirPath;
        }

        private static string GetCalendarFilename() => Path.Combine(GetRunningDirectory(), _calendarJSONFile);
        private static string GetCurrenciesFilename() => Path.Combine(GetRunningDirectory(), _currenciesFile);

        public static IObjectStore<T> GetObjectCache<T>()
        {
            lock (_lock)
            {
                var os = SessionContainer.GetService<IObjectStore<T>>();
                _registeredTypes.AddOrUpdate(typeof(T), os, (x, y) => os);
                return os;
            }
        }
        public static T GetObjectFromCache<T>(string name)
        {
            lock (_lock)
            {
                return SessionContainer.GetService<IObjectStore<T>>().GetObject(name).Value;
            }
        }
        public static void PutObjectToCache<T>(string name, T obj) => GetObjectCache<T>().PutObject(name, new SessionItem<T> { Name = name, Value = obj, Version = 1 });

        public static void FlushCache<T>() => SessionContainer.GetService<IObjectStore<T>>().Clear();
        public static void FlushAllCaches()
        {
            foreach (var t in _registeredTypes.Values.ToArray())
            {
                t.Clear();
            }
        }
    }
}
