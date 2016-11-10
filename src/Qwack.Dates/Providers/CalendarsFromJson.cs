﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Qwack.Dates.Providers
{
    public class CalendarsFromJson: ICalendarProvider
    {
        private static readonly JsonSerializerSettings _jsonSettings;
        private Dictionary<string, Calendar> _loadedCalendars;
        private CalendarCollection _calendarCollection;

        static CalendarsFromJson()
        {
            _jsonSettings = new JsonSerializerSettings();
            _jsonSettings.DateFormatString = "yyyyMMdd";
        }

        private CalendarsFromJson()
        {
        }

        public CalendarCollection Collection => _calendarCollection;
        public Dictionary<string, Calendar> OriginalCalendars => _loadedCalendars;

        public static CalendarsFromJson Parse(string jsonString)
        {
            var returnValue = new CalendarsFromJson();
            returnValue._loadedCalendars = JsonConvert.DeserializeObject<Dictionary<string, Calendar>>(jsonString, _jsonSettings);
            returnValue._calendarCollection = new CalendarCollection(returnValue._loadedCalendars.Values);
            return returnValue;
        }

        public static CalendarsFromJson Load(string filename)
        {
            return Parse(System.IO.File.ReadAllText(filename));
        }
    }
}
