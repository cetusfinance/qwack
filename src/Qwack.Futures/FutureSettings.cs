using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Dates;

namespace Qwack.Futures
{
    public class FutureSettings
    {
        private readonly ICalendarProvider _calendarProvider;

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

        public FutureOptionsGenerator Options { get; set; }

        public FutureSettings(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;

        public override string ToString() => string.Join(",", Names);
        
        public DateTime GetUTCCloseFromDay(DateTime date)
        {
            if (ExpiryGen.NeverExpires)
            {
                return date.Date.Add(new TimeSpan(23, 59, 59));
            }
            if (ExpiryGen.CalenderObject.IsHoliday(date))
            {
                date = date.AddDays(-1).AddPeriod( RollType.F, ExpiryGen.CalenderObject, new Frequency(1, DatePeriodType.B)).Date;
            }

            DateTime tempDate;
            if ((MarketCloseValidTo.Count == 1) && (MarketCloseValidTo[0] == DateTime.MaxValue))
            {
                tempDate = new DateTime(date.Year, date.Month, date.Day, MarketCloseTime[0].Hours, MarketCloseTime[0].Minutes, MarketCloseTime[0].Seconds);
            }
            else
            {
                var q = MarketCloseValidTo.BinarySearch(date);

                if (q < 0)
                    q = ~q;

                tempDate = new DateTime(date.Year, date.Month, date.Day, MarketCloseTime[q].Hours, MarketCloseTime[q].Minutes, MarketCloseTime[q].Seconds);
            }

            return TimeZoneInfo.ConvertTimeToUtc(tempDate, TimeZone);
        }
    }
}
