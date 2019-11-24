using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly List<Currency> _allCurrencies;
        private readonly Dictionary<string, Currency> _currenciesByName;
        private readonly ILogger _logger;

        public CurrenciesFromJson(ICalendarProvider calendarProvider, string fileName, ILoggerFactory loggerFactory)
            :this(calendarProvider, fileName, loggerFactory.CreateLogger<CurrenciesFromJson>())
        {

        }

        public CurrenciesFromJson(ICalendarProvider calendarProvider, string fileName, ILogger<CurrenciesFromJson> logger)
        {
            _logger = logger;
            _calendarProvider = calendarProvider;
            _jsonSettings = new JsonSerializerSettings()
            {
                Converters = new JsonConverter[]
                {
                    new CurrencyConverter(_calendarProvider),
                },
            };
            try
            {
                _allCurrencies = JsonConvert.DeserializeObject<List<Currency>>(System.IO.File.ReadAllText(fileName), _jsonSettings);
                _currenciesByName = _allCurrencies.ToDictionary(c => c.Ccy, c => c, StringComparer.OrdinalIgnoreCase);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to load currencies from Json");
            }
        }

        public Currency this[string ccy] => _currenciesByName[ccy];

        public Currency GetCurrency(string ccy) => TryGetCurrency(ccy, out var C) ? C : throw new Exception($"Currency {ccy} not found in cache");

        public bool TryGetCurrency(string ccy, out Currency output) => _currenciesByName.TryGetValue(ccy, out output);
    }
}
