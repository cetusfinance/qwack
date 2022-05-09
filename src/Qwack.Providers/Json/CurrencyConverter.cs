using System;
using Newtonsoft.Json.Converters;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class CurrencyConverter : CustomCreationConverter<Currency>
    {
        private readonly ICalendarProvider _calendarProvider;

        public CurrencyConverter(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;

        public override Currency Create(Type objectType) => new(_calendarProvider);
    }
}
