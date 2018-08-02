using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Futures
{
    public class FutureSettings
    {
        public List<string> Names { get; set; }
        public FutureDatesGenerator ExpiryGen { get; set; }
        public FutureDatesGenerator RollGen { get; set; }
        public List<string> Months { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public List<TimeSpan> MarketCloseTime { get; set; }

        public List<DateTime> MarketOpenRulesValidUntil { get; set; }
        public List<TimeSpan> MarketOpenTime { get; set; }
        public List<int> MarketOpenModifier { get; set; }
        public List<MarketOpenOverride> MarketOpenTimeOverride { get; set; }

        public List<TimeSpan> MarketCloseVWAPLength { get; set; }
        public List<DateTime> MarketCloseValidTo { get; set; }

        public List<MarketShutRuleSet> MarketShutRules { get; set; }
        public List<DateTime> MarketShutRulesValidUntil { get; set; }

        public double LotSize { get; set; }
        public double PriceMultiplier { get; set; }
        public double TickSize { get; set; }
    }
}
