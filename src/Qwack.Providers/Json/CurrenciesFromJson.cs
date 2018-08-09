using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class CurrenciesFromJson : ICurrencyProvider
    {
        private static readonly JsonSerializerSettings _jsonSettings;
        private Dictionary<string, Currency> _loadedCurrencies;

        static CurrenciesFromJson() => _jsonSettings = new JsonSerializerSettings()
        {
            DateFormatString = "yyyyMMdd"
        };

        public static CurrenciesFromJson Parse(string jsonString, ICalendarProvider calendarProvider)
        {
            var returnValue = new CurrenciesFromJson()
            {
                _loadedCurrencies = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(jsonString, _jsonSettings)
            };
            return returnValue;
        }

        public static CurrenciesFromJson Load(string filename, ICalendarProvider calendarProvider) => Parse(System.IO.File.ReadAllText(filename), calendarProvider);
    }
}
