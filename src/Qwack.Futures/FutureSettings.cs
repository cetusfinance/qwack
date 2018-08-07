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
            if (ExpiryGen.CalendarObject.IsHoliday(date))
            {
                date = date.AddDays(-1).AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B)).Date;
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

        public bool IsOpenFromUTC(DateTime timeToCheck)
        {
            if ((MarketShutRulesValidUntil.Count == 1) && (MarketShutRulesValidUntil[0] == DateTime.MaxValue))
            {
                return MarketShutRules[0].IsOpenFromUTC(timeToCheck);
            }
            else
            {
                var X = MarketShutRulesValidUntil.BinarySearch(timeToCheck);
                if (X < 0)
                    X = ~X;
                return MarketShutRules[X].IsOpenFromUTC(timeToCheck);
            }
        }

        public (DateTime pauseStart, DateTime pauseEnd) GetNextMarketPause(DateTime inDate)
        {
            if ((MarketShutRulesValidUntil.Count == 1) && (MarketShutRulesValidUntil[0] == DateTime.MaxValue))
            {
                return MarketShutRules[0].NextMarketPause(inDate);
            }
            else
            {
                var X = MarketShutRulesValidUntil.BinarySearch(inDate);
                if (X < 0)
                    X = ~X;
                return MarketShutRules[X].NextMarketPause(inDate);
            }
        }

        public DateTime GetNextUTCOpen(DateTime UTCDateTime)
        {
            var U0 = UTCDateTime;

            if (ExpiryGen.CalendarObject.IsHoliday(UTCDateTime))
            {
                UTCDateTime = UTCDateTime.AddDays(-1).AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B)).Date;
            }

            var openToday = GetUTCOpenFromDay(UTCDateTime);
            if ((UTCDateTime < openToday) || (U0 < UTCDateTime && UTCDateTime <= openToday))
                return openToday;
            else
            {
                var tomorrow = UTCDateTime.AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B));
                var nextOpen = GetUTCOpenFromDay(tomorrow);
                if (nextOpen <= U0)
                {
                    tomorrow = tomorrow.AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B));
                    nextOpen = GetUTCOpenFromDay(tomorrow);
                }

                return nextOpen;
            }
        }
        public DateTime GetUTCOpenFromDay(DateTime date)
        {
            DateTime tempDate;
            if ((MarketOpenRulesValidUntil.Count == 1) && (MarketOpenRulesValidUntil[0] == DateTime.MaxValue))
            {
                date = date.AddDays(MarketOpenModifier[0]);
                if (ExpiryGen.CalendarObject.IsHoliday(date))
                {
                    var OOHWNDF = GetOpenOnHolidayWhenNormalDayFollows(date);
                    tempDate = new DateTime(date.Year, date.Month, date.Day, OOHWNDF.Hours, OOHWNDF.Minutes, OOHWNDF.Seconds);
                }
                else
                {
                    tempDate = new DateTime(date.Year, date.Month, date.Day, MarketOpenTime[0].Hours, MarketOpenTime[0].Minutes, MarketOpenTime[0].Seconds);
                }
            }
            else
            {
                var q = MarketOpenRulesValidUntil.BinarySearch(date);

                if (q < 0)
                    q = ~q;

                date = date.AddDays(MarketOpenModifier[q]);
                if (ExpiryGen.CalendarObject.IsHoliday(date))
                {
                    var OOHWNDF = GetOpenOnHolidayWhenNormalDayFollows(date);
                    tempDate = new DateTime(date.Year, date.Month, date.Day, OOHWNDF.Hours, OOHWNDF.Minutes, OOHWNDF.Seconds);
                }
                else
                {
                    tempDate = new DateTime(date.Year, date.Month, date.Day, MarketOpenTime[q].Hours, MarketOpenTime[q].Minutes, MarketOpenTime[q].Seconds);
                }
            }

            return TimeZoneInfo.ConvertTimeToUtc(tempDate, TimeZone);
        }

        public DateTime GetNextUTCClose(DateTime UTCDateTime)
        {
            if (ExpiryGen.CalendarObject.IsHoliday(UTCDateTime))
            {
                UTCDateTime = UTCDateTime.AddDays(-1).AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B)).Date;
            }

            var closeToday = GetUTCCloseFromDay(UTCDateTime);
            if (UTCDateTime < closeToday)
                return closeToday;
            else
            {
                var tomorrow = UTCDateTime.AddPeriod(RollType.F, ExpiryGen.CalendarObject, new Frequency(1, DatePeriodType.B));
                return GetUTCCloseFromDay(tomorrow);
            }
        }

        public TimeSpan GetOpenOnHolidayWhenNormalDayFollows(DateTime valDate)
        {
            if ((MarketShutRules.Count == 1) && (MarketShutRulesValidUntil[0] == DateTime.MaxValue))
            {
                return MarketShutRules[0].OpenOnHolidayWhenNormalDayFollows;
            }
            else
            {
                var q = MarketShutRulesValidUntil.BinarySearch(valDate);

                if (q < 0)
                    q = ~q;

                return MarketShutRules[q].OpenOnHolidayWhenNormalDayFollows;
            }
        }
    }
}
