using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Dates;
using Qwack.Providers;
using System.Reflection;
using System.IO;

namespace Qwack.Excel.Services
{
    public class StaticDataService
    {
        private const string _calendarJSONFile = "Calendars.json";
        private static StaticDataService _instance;
        private static readonly object _lock = new object();
        private StaticDataService() { }

        public static StaticDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new StaticDataService();
                        }
                    }
                }
                return _instance;
            }
        }

        private ICalendarProvider _calendarProvider;
        public ICalendarProvider CalendarProvider
        {
            get
            {
                if(_calendarProvider==null)
                {
                    lock(_lock)
                    {
                        if(_calendarProvider==null)
                        {
                            _calendarProvider = Json.Providers.CalendarsFromJson.Load(GetCalendarFilename());
                        }
                    }
                }
                return _calendarProvider;
            }
        }

        private static string GetCalendarFilename()
        {
            var assemblyLocation = Assembly.GetAssembly(typeof(Calendar)).Location;
            return Path.Combine(Path.GetDirectoryName(assemblyLocation), _calendarJSONFile);
        }
    
    }
}
