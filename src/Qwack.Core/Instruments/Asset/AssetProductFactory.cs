using System;
using System.Collections.Generic;
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
                    PaymentCalendar = payCalendar,
                    PaymentLag = payOffset,
                    PaymentDate = m.LastDayOfMonth().AddPeriod(RollType.F, fixingCalendar, payOffset),
                    PaymentCurrency = currency,
                    Direction = tradeDirection,
                    Notional = notional
                });
                m = m.AddMonths(1);
            }
            return new AsianSwapStrip { Swaplets = swaplets.ToArray() };
        }

        public static AsianOption CreatAsianOption(string period, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    Start.BusinessDaysInPeriod(End.LastDayOfMonth(), fixingCalendar) :
                    Start.FridaysInPeriod(End.LastDayOfMonth(), fixingCalendar);
            return new AsianOption
                {
                AssetId = assetId,
                    AverageStartDate = Start,
                    AverageEndDate = End,
                    FixingCalendar = fixingCalendar,
                    Strike = strike,
                    FixingDates = fixingDates.ToArray(),
                    CallPut = putCall,
                    PaymentCalendar = payCalendar,
                    PaymentLag = payOffset,
                    PaymentDate = End.AddPeriod(RollType.F, fixingCalendar, payOffset),
                    PaymentCurrency = currency,
                    Direction = tradeDirection,
                    Notional = notional
                };

        }
    }
}
