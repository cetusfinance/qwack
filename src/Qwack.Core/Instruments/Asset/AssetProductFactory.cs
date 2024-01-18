using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Asset
{
    public static class AssetProductFactory
    {
        public static AsianSwapStrip CreateMonthlyAsianSwap(string period, double strike, string assetId, Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
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
            var fixingDates = start == end ? new List<DateTime> { start } :
                fixingDateType == DateGenerationType.BusinessDays ?
                   start.BusinessDaysInPeriod(end, fixingCalendar) :
                   start.FridaysInPeriod(end, fixingCalendar);

            if (!fixingDates.Any() && start == end) //hack for bullet swaps where system returns fixing date on holiday
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

        public static AsianBasisSwap CreateTermAsianBasisSwap(string period, double leg2PremiumInleg1Units, SwapPayReceiveType leg1PayRec, string assetIdLeg1, string assetIdLeg2, Calendar fixingCalendarLeg1, Calendar fixingCalendarLeg2, Calendar payCalendar, Frequency payOffset, Currency currency, Frequency spotLagLeg1 = new Frequency(), Frequency spotLagLeg2 = new Frequency(), double notionalLeg1 = 1, double notionalLeg2 = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var (Start, End) = period.ParsePeriod();
            return CreateTermAsianBasisSwap(Start, End, leg2PremiumInleg1Units, leg1PayRec, assetIdLeg1, assetIdLeg2, fixingCalendarLeg1, fixingCalendarLeg2, payCalendar, payOffset, currency, spotLagLeg1, spotLagLeg2, notionalLeg1, notionalLeg2, fixingDateType);
        }

        public static AsianBasisSwap CreateTermAsianBasisSwap(DateTime start, DateTime end, double leg2PremiumInleg1Units, SwapPayReceiveType leg1PayRec, string assetIdLeg1, string assetIdLeg2, Calendar fixingCalendarLeg1, Calendar fixingCalendarLeg2, Calendar payCalendar, Frequency payOffset, Currency currency, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalLeg1 = 1, double notionalLeg2 = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var payDate = end.AddPeriod(RollType.F, payCalendar, payOffset);
            return CreateTermAsianBasisSwap(start, end, leg2PremiumInleg1Units, leg1PayRec, assetIdLeg1, assetIdLeg2, fixingCalendarLeg1, fixingCalendarLeg2, payDate, currency, spotLagPay, spotLagRec, notionalLeg1, notionalLeg2, fixingDateType);
        }

        public static AsianBasisSwap CreateTermAsianBasisSwap(DateTime start, DateTime end, double leg2PremiumInleg1Units, SwapPayReceiveType leg1PayRec, string assetIdLeg1, string assetIdLeg2, Calendar fixingCalendarLeg1, Calendar fixingCalendarLeg2, DateTime payDate, Currency currency, Frequency spotLagLeg1 = new Frequency(), Frequency spotLagLeg2 = new Frequency(), double notionalLeg1 = 1, double notionalLeg2 = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var swapLeg1 = CreateTermAsianSwap(start, end, -leg2PremiumInleg1Units, assetIdLeg1, fixingCalendarLeg1, payDate, currency, leg1PayRec==SwapPayReceiveType.Rec ? TradeDirection.Long : TradeDirection.Short, spotLagLeg1, notionalLeg1);
            var swapLeg2 = CreateTermAsianSwap(start, end, 0, assetIdLeg2, fixingCalendarLeg2, payDate, currency, leg1PayRec == SwapPayReceiveType.Pay ? TradeDirection.Long : TradeDirection.Short, spotLagLeg2, notionalLeg2);

            (var pay, var rec) = leg1PayRec == SwapPayReceiveType.Payer ? (swapLeg1, swapLeg2) : (swapLeg2, swapLeg1);
            var swap = new AsianBasisSwap
            {
                PaySwaplets = new[] { pay },
                RecSwaplets = new[] { rec },
            };

            return swap;
        }

        public static AssetFxBasisSwap CreateAssetFxBasisSwap(DateTime start, DateTime end, string assetId, double payPremium, double recPremium, Currency payCcy, Currency recCcy, Calendar fixingCalendarPay, Calendar fixingCalendarRec, Frequency settleLag, Calendar settleCalendar, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalPay = 1, double notionalRec = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var settleDate = end.AddPeriod(RollType.F, settleCalendar, settleLag);
            return CreateAssetFxBasisSwap(start, end, assetId, payPremium, recPremium, payCcy, recCcy, fixingCalendarPay, fixingCalendarRec, settleDate, spotLagPay, spotLagRec, notionalPay, notionalRec, fixingDateType);
        }

        public static AssetFxBasisSwap CreateAssetFxBasisSwap(DateTime start, DateTime end, string assetId, double payPremium, double recPremium, Currency payCcy, Currency recCcy, Calendar fixingCalendarPay, Calendar fixingCalendarRec, DateTime settleDate, Frequency spotLagPay = new Frequency(), Frequency spotLagRec = new Frequency(), double notionalPay = 1, double notionalRec = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var swapLegPay = CreateTermAsianSwap(start, end, payPremium, assetId, fixingCalendarPay, settleDate, payCcy, TradeDirection.Long, spotLagPay, notionalPay);
            var swapLegRec = CreateTermAsianSwap(start, end, recPremium, assetId, fixingCalendarRec, settleDate, recCcy, TradeDirection.Short, spotLagRec, notionalRec);

          
            var swap = new AssetFxBasisSwap
            {
                PaySwaplet = swapLegPay,
                RecSwaplet = swapLegRec
            };

            return swap;
        }

        public static AsianBasisSwap CreateBulletBasisSwap(DateTime leg1Fixing, DateTime leg2Fixing, double leg2PremiumInleg1Units, SwapPayReceiveType leg1PayRec, string assetIdLeg1, string assetIdLeg2, Currency currency, double notionalLeg1, double notionalLeg2)
        {
            var payDate = leg1Fixing.Max(leg2Fixing);
            var swapLeg1 = new Forward
            {
                AssetId = assetIdLeg1,
                ExpiryDate = leg1Fixing,
                PaymentDate = payDate,
                Notional = notionalLeg1,
                Direction = leg1PayRec == SwapPayReceiveType.Payer ? TradeDirection.Short : TradeDirection.Long,
                Strike = -leg2PremiumInleg1Units,
            }.AsBulletSwap();
            var swapLeg2 = new Forward
            {
                AssetId = assetIdLeg2,
                ExpiryDate = leg2Fixing,
                PaymentDate = payDate,
                Notional = notionalLeg2,
                Direction = leg1PayRec == SwapPayReceiveType.Payer ? TradeDirection.Long : TradeDirection.Short,
                Strike = 0.0,
            }.AsBulletSwap();
            (var pay, var rec) = leg1PayRec == SwapPayReceiveType.Payer ? (swapLeg1, swapLeg2) : (swapLeg2, swapLeg1);

            var swap = new AsianBasisSwap
            {
                PaySwaplets = new[] { pay },
                RecSwaplets = new[] { rec },
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

            var fixingDates = start == end ? new List<DateTime> { start } :
                fixingDateType == DateGenerationType.BusinessDays ?
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

            var fixingDates = start == end ? new List<DateTime> { start } :
                fixingDateType == DateGenerationType.BusinessDays ?
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

        public static MultiPeriodBackpricingOption CreateMultiPeriodBackPricingOption(Tuple<DateTime, DateTime>[] periodDates, DateTime decision, string assetId, OptionType putCall, Calendar fixingCalendar, DateTime payDate, Currency currency, TradeDirection tradeDirection = TradeDirection.Long, Frequency spotLag = new Frequency(), double notional = 1, DateGenerationType fixingDateType = DateGenerationType.BusinessDays)
        {
            var fixingDates = fixingDateType == DateGenerationType.BusinessDays ?
                    periodDates.Select(pd => pd.Item1.BusinessDaysInPeriod(pd.Item2, fixingCalendar).ToArray()).ToList() :
                    periodDates.Select(pd => pd.Item1.FridaysInPeriod(pd.Item2, fixingCalendar).ToArray()).ToList();
            return new MultiPeriodBackpricingOption
            {
                AssetId = assetId,
                PeriodDates = periodDates,
                DecisionDate = decision,
                FixingCalendar = fixingCalendar,
                FixingDates = fixingDates,
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
    }
}
