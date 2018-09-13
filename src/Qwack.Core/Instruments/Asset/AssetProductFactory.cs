using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public static class AssetProductFactory
    {
        public static AsianSwapStrip CreateMonthlyAsianSwap(string period, double strike, string assetId, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection=TradeDirection.Long, Frequency spotLag = new Frequency(), double notional=1, DateGenerationType fixingDateType=DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateMonthlyAsianSwap(Start, End, strike, assetId, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }

        public static AsianSwapStrip CreateMonthlyAsianSwap(DateTime start, DateTime end, double strike, string assetId, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var m = start;
            var swaplets = new List<AsianSwap>();
            if (start.Month + start.Year * 12 == end.Month + end.Year * 12)
            {
                var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                       start.BusinessDaysInPeriod(end, fixingCalendar) :
                       start.FridaysInPeriod(end, fixingCalendar);

                swaplets.Add(new AsianSwap
                {
                    AssetId = assetId,
                    AverageStartDate = start,
                    AverageEndDate = end,
                    FixingCalendar = fixingCalendar,
                    Strike = strike,
                    FixingDates = fixingDates.ToArray(),
                    SpotLag = spotLag,
                    PaymentCalendar = payCalendar,
                    PaymentLag = payOffset,
                    PaymentDate = end.AddPeriod(RollType.F, fixingCalendar, payOffset),
                    PaymentCurrency = currency,
                    Direction = tradeDirection,
                    Notional = notional
                });
            }
            else
            {
                while ((m.Month + m.Year * 12) <= (end.Month + end.Year * 12))
                {
                    var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                        m.BusinessDaysInPeriod(m.LastDayOfMonth(), fixingCalendar) :
                        m.FridaysInPeriod(m.LastDayOfMonth(), fixingCalendar);

                    swaplets.Add(new AsianSwap
                    {
                        AssetId = assetId,
                        AverageStartDate = m,
                        AverageEndDate = m.LastDayOfMonth(),
                        FixingCalendar = fixingCalendar,
                        Strike = strike,
                        FixingDates = fixingDates.ToArray(),
                        SpotLag = spotLag,
                        PaymentCalendar = payCalendar,
                        PaymentLag = payOffset,
                        PaymentDate = m.LastDayOfMonth().AddPeriod(RollType.F, fixingCalendar, payOffset),
                        PaymentCurrency = currency,
                        Direction = tradeDirection,
                        Notional = notional
                    });
                    m = m.LastDayOfMonth().AddDays(1);
                }
            }
            return new AsianSwapStrip { Swaplets = swaplets.ToArray() };
        }

        public static AsianSwap CreateTermAsianSwap(string period, double strike, string assetId, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateTermAsianSwap(Start, End, strike, assetId, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }

        public static AsianSwap CreateTermAsianSwap(DateTime start, DateTime end, double strike, string assetId, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var payDate = end.AddPeriod(RollType.F, fixingCalendar, payOffset);
            return CreateTermAsianSwap(start, end, strike, assetId, fixingCalendar, payDate, currency, tradeDirection, spotLag, notional, fixingDateType);
        }

        public static AsianSwap CreateTermAsianSwap(DateTime start, DateTime end, double strike, string assetId, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                   start.BusinessDaysInPeriod(end, fixingCalendar) :
                   start.FridaysInPeriod(end, fixingCalendar);

            if(!fixingDates.Any() && start==end) //hack for bullet swaps where system returns fixing date on holiday
            {
                start = start.IfHolidayRollForward(fixingCalendar);
                end = start;
                fixingDates.Add(start);
            }
            var swap = new AsianSwap
            {
                AssetId = assetId,
                AverageStartDate = start,
                AverageEndDate = end,
                FixingCalendar = fixingCalendar,
                Strike = strike,
                SpotLag = spotLag,
                FixingDates = fixingDates.ToArray(),
                PaymentDate = payDate,
                PaymentCurrency = currency,
                Direction = tradeDirection,
                Notional = notional
            };

            return swap;
        }


        public static AsianOption CreatAsianOption(string period, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreatAsianOption(Start, End, strike, assetId, putCall, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }


        public static AsianOption CreatAsianOption(DateTime start, DateTime end, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {

            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end.LastDayOfMonth(), fixingCalendar) :
                    start.FridaysInPeriod(end.LastDayOfMonth(), fixingCalendar);
            return new AsianOption
            {
                AssetId = assetId,
                AverageStartDate = start,
                AverageEndDate = end,
                FixingCalendar = fixingCalendar,
                Strike = strike,
                FixingDates = fixingDates.ToArray(),
                SpotLag = spotLag,
                CallPut = putCall,
                PaymentCalendar = payCalendar,
                PaymentLag = payOffset,
                PaymentDate = end.AddPeriod(RollType.F, fixingCalendar, payOffset),
                PaymentCurrency = currency,
                Direction = tradeDirection,
                Notional = notional
            };

        }

        public static AsianOption CreatAsianOption(DateTime start, DateTime end, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {

            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end.LastDayOfMonth(), fixingCalendar) :
                    start.FridaysInPeriod(end.LastDayOfMonth(), fixingCalendar);
            return new AsianOption
            {
                AssetId = assetId,
                AverageStartDate = start,
                AverageEndDate = end,
                FixingCalendar = fixingCalendar,
                Strike = strike,
                FixingDates = fixingDates.ToArray(),
                SpotLag = spotLag,
                CallPut = putCall,
                PaymentDate = payDate,
                PaymentCurrency = currency,
                Direction = tradeDirection,
                Notional = notional
            };

        }
    }
}
