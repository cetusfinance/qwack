using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Qwack.Core.Instruments.Futures;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class FutureSettingsFromJson: IFutureSettingsProvider
    {
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly ICalendarProvider _calendarProvider;

        private FutureSettingsFromJson() => _jsonSettings = new JsonSerializerSettings()
        {
            DateFormatString = "yyyyMMdd",
            Converters = new JsonConverter[]
            {
                new MarketShutRuleSetConverter(_calendarProvider),
            },
        };
                                
        public FutureSettingsFromJson(ICalendarProvider calendarProvider, string fileName)
        {
            _calendarProvider = calendarProvider;
            var filename = @"C:\code\FinanceLibrary\FinanceLibrary\Settings\FutureDateSettings.xml";
            var xdoc = XDocument.Load(filename);
            var list = new List<FutureSettings>();
            foreach (var topelment in xdoc.Elements().First().Elements())
            {
                var future = new FutureSettings(calendarProvider);
                future.LoadXml(topelment);
                list.Add(future);
            }

            var output = JsonConvert.SerializeObject(list);
            System.IO.File.WriteAllText("c:\\code\\futuresettings.json", output);

            var newList = JsonConvert.DeserializeObject<List<FutureSettings>>(output, _jsonSettings);
        }
    }
}
