using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Excel.Utils;

namespace Qwack.Excel
{
    public static class ContainerStores
    {
        private const string _calendarJSONFile = "Calendars.json";

        static ContainerStores()
        {
            GlobalContainer = new ServiceCollection()
             .AddLogging()
             .AddSingleton<ICalendarProvider>(Json.Providers.CalendarsFromJson.Load(GetCalendarFilename()))
             .AddSingleton(typeof(IObjectStore<>), typeof(ExcelObjectStore<>))
             .BuildServiceProvider();

            SessionContainer = GlobalContainer.CreateScope().ServiceProvider;
        }
        
        public static IServiceProvider GlobalContainer { get; internal set; }
        public static IServiceProvider SessionContainer { get;set;}

        private static string GetCalendarFilename()
        {
            var assemblyLocation = Assembly.GetAssembly(typeof(Calendar)).Location;
            return Path.Combine(Path.GetDirectoryName(assemblyLocation), _calendarJSONFile);
        }

        public static IObjectStore<T> GetObjectCache<T>()
        {
            return SessionContainer.GetService<IObjectStore<T>>();
        }
    }
}
