using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Qwack.Dates;
using Qwack.Futures;

namespace Qwack.Providers.Json
{
    public class FutureSettingsFromJson : IFutureSettingsProvider
    {
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly ICalendarProvider _calendarProvider;
        private readonly List<FutureSettings> _allFutureSettings;
        private readonly Dictionary<string, FutureSettings> _allSettingsByName = new Dictionary<string, FutureSettings>(StringComparer.OrdinalIgnoreCase);
        private ILogger _logger;

        public FutureSettingsFromJson(ICalendarProvider calendarProvider, string fileName, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FutureSettingsFromJson>();
            _calendarProvider = calendarProvider;
            _jsonSettings = new JsonSerializerSettings()
            {
                DateFormatString = "yyyyMMdd",
                Converters = new JsonConverter[]
                {
                    new MarketShutRuleSetConverter(_calendarProvider),
                    new FutureDatesGeneratorConverter(_calendarProvider),
                },
            };
            try
            {
                _allFutureSettings = JsonConvert.DeserializeObject<List<FutureSettings>>(System.IO.File.ReadAllText(fileName), _jsonSettings);
                foreach (var fs in _allFutureSettings)
                {
                    foreach (var n in fs.Names)
                    {
                        _allSettingsByName.Add(n, fs);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error loading future settings from json");
            }
        }

        public FutureSettings this[string futureName] => _allSettingsByName[futureName];

        public bool TryGet(string futureName, out FutureSettings futureSettings) => _allSettingsByName.TryGetValue(futureName, out futureSettings);
    }
}
