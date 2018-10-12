using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Serialization;

namespace Qwack.Providers.Json
{
    public class CurrenciesFromJson : ICurrencyProvider
    {
        [SkipSerialization]
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly ICalendarProvider _calendarProvider;
        private List<Currency> _allCurrencies;
        private readonly Dictionary<string, Currency> _currenciesByName;

        public CurrenciesFromJson(ICalendarProvider calendarProvider, string fileName)
        {
            _calendarProvider = calendarProvider;
            _jsonSettings = new JsonSerializerSettings()
            {
                Converters = new JsonConverter[]
                {
                    new CurrencyConverter(_calendarProvider),
                },
            };
            _allCurrencies = JsonConvert.DeserializeObject<List<Currency>>(System.IO.File.ReadAllText(fileName), _jsonSettings);
            _currenciesByName = _allCurrencies.ToDictionary(c => c.Ccy, c => c, StringComparer.OrdinalIgnoreCase);
        }

        public Currency this[string ccy] => _currenciesByName[ccy];

        public bool TryGetCurrency(string ccy, out Currency output) => _currenciesByName.TryGetValue(ccy, out output);
    }
}
