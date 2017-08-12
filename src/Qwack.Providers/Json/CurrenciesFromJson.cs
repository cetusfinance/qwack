using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class CurrenciesFromJson : ICurrencyProvider
    {
        public static CurrenciesFromJson Parse(string jsonString, ICalendarProvider calendarProvider) => throw new NotImplementedException();

        public static CurrenciesFromJson Load(string filename, ICalendarProvider calendarProvider) => Parse(System.IO.File.ReadAllText(filename), calendarProvider);
    }
}
