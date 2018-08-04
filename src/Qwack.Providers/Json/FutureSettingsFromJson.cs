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
        private static readonly JsonSerializerSettings _jsonSettings;
        
        static FutureSettingsFromJson() => _jsonSettings = new JsonSerializerSettings()
        {
            DateFormatString = "yyyyMMdd"
        };
                                
        public FutureSettingsFromJson(ICalendarProvider calendarProvider, string fileName)
        {
            var filename = @"C:\code\FinanceLibrary\FinanceLibrary\Settings\FutureDateSettings.xml";
            var xdoc = XDocument.Load(filename);
            var list = new List<FutureSettings>();
            foreach (var topelment in xdoc.Elements().First().Elements())
            {
                var future = new FutureSettings(calendarProvider);
                future.LoadXml(topelment);
                list.Add(future);
            }
        }
    }
}
