﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Dates;
using Qwack.Providers;

namespace Qwack.Excel.Services
{
    public class StaticDataService
    {
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
                            _calendarProvider = Json.Providers.CalendarsFromJson.Load(null);
                        }
                    }
                }
                return _calendarProvider;
            }
        }
    }
}
