using System;
using Newtonsoft.Json.Converters;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public class MarketShutRuleSetConverter : CustomCreationConverter<MarketShutRuleSet>
    {
        private readonly ICalendarProvider _calendarProvider;

        public MarketShutRuleSetConverter(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;

        public override MarketShutRuleSet Create(Type objectType) => new(_calendarProvider);
    }

    public class FutureDatesGeneratorConverter : CustomCreationConverter<FutureDatesGenerator>
    {
        private readonly ICalendarProvider _calendarProvider;

        public FutureDatesGeneratorConverter(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;

        public override FutureDatesGenerator Create(Type objectType) => new(_calendarProvider);
    }
}
