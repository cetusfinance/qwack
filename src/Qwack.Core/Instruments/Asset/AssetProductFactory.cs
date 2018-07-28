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
            var pDates = period.ParsePeriod();
            var m = pDates.Start;
            var swaplets = new List<AsianSwap>();
            while ((m.Month+m.Year*12)<=(pDates.End.Month+pDates.End.Year*12))
            {
                var fixingDates = fixingDateType==DateGenerationType.BusinessDays?
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
    }
}
