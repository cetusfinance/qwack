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
                    Notional = notional,
                    FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
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
                        Notional = notional,
                        FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
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
            var payDate = end.AddPeriod(RollType.F, payCalendar, payOffset);
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
                Notional = notional,
                FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
            };

            return swap;
        }

        public static AsianBasisSwap CreateTermAsianBasisSwap(string period, double strike, string assetIdPay, string assetIdRec, Calendar fixingCalendarPay, Calendar fixingCalendarRec, Calendar payCalendar, Frequency payOffset, Currency currency, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalPay = 1, double notionalRec = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateTermAsianBasisSwap(Start, End, strike, assetIdPay, assetIdRec, fixingCalendarPay, fixingCalendarRec, payCalendar, payOffset, currency, spotLagPay, spotLagRec, notionalPay, notionalRec, fixingDateType);
        }

        public static AsianBasisSwap CreateTermAsianBasisSwap(DateTime start, DateTime end, double strike, string assetIdPay, string assetIdRec, Calendar fixingCalendarPay, Calendar fixingCalendarRec, Calendar payCalendar, Frequency payOffset, Currency currency, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalPay = 1, double notionalRec = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var payDate = end.AddPeriod(RollType.F, payCalendar, payOffset);
            return CreateTermAsianBasisSwap(start, end, strike, assetIdPay, assetIdRec, fixingCalendarPay, fixingCalendarRec, payDate, currency, spotLagPay, spotLagRec, notionalPay, notionalRec, fixingDateType);
        }

        public static AsianBasisSwap CreateTermAsianBasisSwap(DateTime start, DateTime end, double strike, string assetIdPay, string assetIdRec, Calendar fixingCalendarPay, Calendar fixingCalendarRec, DateTime payDate, Currency currency, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalPay = 1, double notionalRec = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var swapPay = CreateTermAsianSwap(start, end, -strike, assetIdPay, fixingCalendarPay, payDate, currency, TradeDirection.Long, spotLagPay, notionalPay);
            var swapRec = CreateTermAsianSwap(start, end, 0, assetIdRec, fixingCalendarRec, payDate, currency, TradeDirection.Short, spotLagRec, notionalRec);

            var swap = new AsianBasisSwap
            {
                PaySwaplets = new [] {swapPay},
                RecSwaplets = new [] {swapRec},
            };

            return swap;
        }

        public static AsianBasisSwap CreateBulletBasisSwap(DateTime payFixing, DateTime recFixing, double strike, string assetIdPay, string assetIdRec, Currency currency, double notionalPay, double notionalRec)
        {
            var payDate = payFixing.Max(recFixing);
            var swapPay = new Forward
            {
                AssetId = assetIdPay,
                ExpiryDate = payFixing,
                PaymentDate = payDate,
                Notional = notionalPay,
                Strike = strike,
            }.AsBulletSwap();
            var swapRec = new Forward
            {
                AssetId = assetIdRec,
                ExpiryDate = recFixing,
                PaymentDate = payDate,
                Notional = notionalRec,
                Strike = 0.0,
            }.AsBulletSwap();

            var swap = new AsianBasisSwap
            {
                PaySwaplets = new[] { swapPay },
                RecSwaplets = new[] { swapRec },
            };

            return swap;
        }

        public static AsianOption CreateAsianOption(string period, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateAsianOption(Start, End, strike, assetId, putCall, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }


        public static AsianOption CreateAsianOption(DateTime start, DateTime end, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {

            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end, fixingCalendar) :
                    start.FridaysInPeriod(end, fixingCalendar);
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
                Notional = notional,
                FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
            };
        }

        public static AsianOption CreateAsianOption(DateTime start, DateTime end, double strike, string assetId, OptionType putCall, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {

            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end, fixingCalendar) :
                    start.FridaysInPeriod(end, fixingCalendar);
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
                Notional = notional,
                FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
            };

        }

        public static AsianLookbackOption CreateAsianLookbackOption(DateTime start, DateTime end, string assetId, OptionType putCall, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end, fixingCalendar) :
                    start.FridaysInPeriod(end, fixingCalendar);
            return new AsianLookbackOption
            {
                AssetId = assetId,
                ObsStartDate = start,
                ObsEndDate = end,
                FixingCalendar = fixingCalendar,
                FixingDates = fixingDates.ToArray(),
                SpotLag = spotLag,
                CallPut = putCall,
                PaymentDate = payDate,
                PaymentCurrency = currency,
                Direction = tradeDirection,
                Notional = notional,
                FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
            };
        }
        public static AsianLookbackOption CreateAsianLookbackOption(DateTime start, DateTime end, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var payDate = end.AddPeriod(RollType.F, fixingCalendar, payOffset);
            return CreateAsianLookbackOption(start, end, assetId, putCall, fixingCalendar, payDate, currency, tradeDirection, spotLag, notional, fixingDateType);
        }
        public static AsianLookbackOption CreateAsianLookbackOption(string period, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateAsianLookbackOption(Start, End, assetId, putCall, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }

        public static BackPricingOption CreateBackPricingOption(DateTime start, DateTime end, DateTime decision, string assetId, OptionType putCall, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    start.BusinessDaysInPeriod(end, fixingCalendar) :
                    start.FridaysInPeriod(end, fixingCalendar);
            return new BackPricingOption
            {
                AssetId = assetId,
                StartDate = start,
                EndDate = end,
                DecisionDate = decision,
                FixingCalendar = fixingCalendar,
                FixingDates = fixingDates.ToArray(),
                SpotLag = spotLag,
                CallPut = putCall,
                PaymentDate = payDate,
                SettlementDate = payDate,
                PaymentCurrency = currency,
                Direction = tradeDirection,
                Notional = notional,
                FxConversionType = currency.Ccy == "USD" ? FxConversionType.None : FxConversionType.AverageThenConvert
            };
        }
        public static BackPricingOption CreateBackPricingOption(DateTime start, DateTime end, DateTime decision, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var payDate = end.AddPeriod(RollType.F, fixingCalendar, payOffset);
            return CreateBackPricingOption(start, end, decision, assetId, putCall, fixingCalendar, payDate, currency, tradeDirection, spotLag, notional, fixingDateType);
        }
        public static BackPricingOption CreateBackPricingOption(string period, string assetId, OptionType putCall, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateBackPricingOption(Start, End, End, assetId, putCall, fixingCalendar, payCalendar, payOffset, currency, tradeDirection, spotLag, notional, fixingDateType);
        }
    }
}
