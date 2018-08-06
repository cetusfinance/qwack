using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

        public FutureSettingsFromJson(ICalendarProvider calendarProvider, string fileName)
        {
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
            _allFutureSettings = JsonConvert.DeserializeObject<List<FutureSettings>>(System.IO.File.ReadAllText(fileName), _jsonSettings);
            foreach (var fs in _allFutureSettings)
            {
                foreach (var n in fs.Names)
                {
                    _allSettingsByName.Add(n, fs);
                }
            }
        }

        public FutureSettings this[string futureName] => _allSettingsByName[futureName];

        public bool TryGet(string futureName, out FutureSettings futureSettings) => _allSettingsByName.TryGetValue(futureName, out futureSettings);
    }
}
