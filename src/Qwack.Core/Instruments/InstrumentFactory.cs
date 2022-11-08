using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments
{
    public static class InstrumentFactory
    {
        public static IInstrument GetInstrument(this TO_Instrument transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            if (transportObject.AssetInstrumentType != AssetInstrumentType.None)
            {
                switch (transportObject.AssetInstrumentType)
                {
                    case AssetInstrumentType.AsianSwap:
                        return transportObject.AsianSwap.GetAsianSwap(currencyProvider, calendarProvider);
                    case AssetInstrumentType.AsianSwapStrip:
                        return new AsianSwapStrip
                        {
                            TradeId = transportObject.AsianSwapStrip.TradeId,
                            Counterparty = transportObject.AsianSwapStrip.Counterparty,
                            PortfolioName = transportObject.AsianSwapStrip.PortfolioName,
                            Swaplets = transportObject.AsianSwapStrip.Swaplets.Select(x => x.GetAsianSwap(currencyProvider, calendarProvider)).ToArray(),
                            HedgingSet = transportObject.AsianSwapStrip.HedgingSet,
                        };
                    case AssetInstrumentType.AsianOption:
                        var ao = GetAsianOption(transportObject.AsianOption, currencyProvider, calendarProvider);
                        return ao;
                    case AssetInstrumentType.Forward:
                        return transportObject.Forward.GetForward(currencyProvider, calendarProvider);
                    case AssetInstrumentType.Equity:
                        return new Equity(transportObject.Equity, currencyProvider, calendarProvider);
                    case AssetInstrumentType.Bond:
                        return new Bond(transportObject.Bond, currencyProvider, calendarProvider);
                    case AssetInstrumentType.FuturesOption:
                        return new FuturesOption(transportObject.FuturesOption, currencyProvider);
                    case AssetInstrumentType.Future:
                        return new Future(transportObject.Future, currencyProvider);
                    case AssetInstrumentType.CashWrapper:
                        return new CashWrapper(transportObject.CashWrapper, currencyProvider, calendarProvider);
                }
            }
            else if(transportObject.FundingInstrumentType != FundingInstrumentType.None)
            {
                switch (transportObject.FundingInstrumentType)
                {
                    case FundingInstrumentType.FxForward:
                        var to1 = transportObject.FxForward;
                        return new FxForward()
                        {
                            Counterparty = to1.Counterparty,
                            DeliveryDate = to1.DeliveryDate,
                            DomesticCCY = currencyProvider.GetCurrencySafe(to1.DomesticCCY),
                            ForeignCCY = currencyProvider.GetCurrencySafe(to1.ForeignCCY),
                            DomesticQuantity = to1.DomesticQuantity,
                            ForeignDiscountCurve = to1.ForeignDiscountCurve,
                            HedgingSet = to1.HedgingSet,
                            MetaData = to1.MetaData,
                            PillarDate = to1.PillarDate,
                            PortfolioName = to1.PortfolioName,
                            SolveCurve = to1.SolveCurve,
                            Strike = to1.Strike,
                            TradeId = to1.TradeId 
                        };
                    case FundingInstrumentType.FxVanillaOption:
                        var to2 = transportObject.FxOption;
                        return new FxVanillaOption(currencyProvider, calendarProvider)
                        {
                            Counterparty = to2.Counterparty,
                            DeliveryDate = to2.DeliveryDate,
                            DomesticCCY = currencyProvider.GetCurrencySafe(to2.DomesticCCY),
                            ForeignCCY = currencyProvider.GetCurrencySafe(to2.ForeignCCY),
                            DomesticQuantity = to2.DomesticQuantity,
                            ForeignDiscountCurve = to2.ForeignDiscountCurve,
                            HedgingSet = to2.HedgingSet,
                            MetaData = to2.MetaData,
                            PillarDate = to2.PillarDate,
                            PortfolioName = to2.PortfolioName,
                            SolveCurve = to2.SolveCurve,
                            Strike = to2.Strike,
                            TradeId = to2.TradeId,
                            CallPut = to2.CallPut,
                            ExpiryDate = to2.ExpiryDate,
                            Premium = to2.Premium,
                            PremiumDate = to2.PremiumDate,
                        };
                }
            }

            throw new Exception("Unable to re-constitute object");
        }

        public static Portfolio GetPortfolio(this TO_Portfolio transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new()
        {
            PortfolioName = transportObject.PortfolioName,
            Instruments = transportObject.Instruments.Select(x => x.GetInstrument(currencyProvider, calendarProvider)).ToList()
        };

        private static AsianSwap GetAsianSwap(this TO_AsianSwap transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new()
        {
            TradeId = transportObject.TradeId,
            Notional = transportObject.Notional,
            Direction = transportObject.Direction,
            AverageStartDate = transportObject.AverageStartDate,
            AverageEndDate = transportObject.AverageEndDate,
            FixingDates = transportObject.FixingDates,
            FixingCalendar = calendarProvider.GetCalendarSafe(transportObject.FixingCalendar),
            PaymentCalendar = calendarProvider.GetCalendarSafe(transportObject.PaymentCalendar),
            SpotLag = new Frequency(transportObject.SpotLag),
            SpotLagRollType = transportObject.SpotLagRollType,
            PaymentLag = new Frequency(transportObject.PaymentLag),
            PaymentLagRollType = transportObject.PaymentLagRollType,
            PaymentDate = transportObject.PaymentDate,
            PaymentCurrency = currencyProvider.GetCurrencySafe(transportObject.PaymentCurrency),
            AssetFixingId = transportObject.AssetFixingId,
            AssetId = transportObject.AssetId,
            DiscountCurve = transportObject.DiscountCurve,
            FxConversionType = transportObject.FxConversionType,
            FxFixingDates = transportObject.FxFixingDates,
            FxFixingId = transportObject.FxFixingId,
            Strike = transportObject.Strike,
            Counterparty = transportObject.Counterparty,
            HedgingSet = transportObject.HedgingSet,
            PortfolioName = transportObject.PortfolioName,
        };

        private static AsianOption GetAsianOption(this TO_AsianOption transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new()
        {
            TradeId = transportObject.TradeId,
            Notional = transportObject.Notional,
            Direction = transportObject.Direction,
            AverageStartDate = transportObject.AverageStartDate,
            AverageEndDate = transportObject.AverageEndDate,
            FixingDates = transportObject.FixingDates,
            FixingCalendar = calendarProvider.GetCalendarSafe(transportObject.FixingCalendar),
            PaymentCalendar = calendarProvider.GetCalendarSafe(transportObject.PaymentCalendar),
            SpotLag = new Frequency(transportObject.SpotLag),
            SpotLagRollType = transportObject.SpotLagRollType,
            PaymentLag = new Frequency(transportObject.PaymentLag),
            PaymentLagRollType = transportObject.PaymentLagRollType,
            PaymentDate = transportObject.PaymentDate,
            PaymentCurrency = currencyProvider.GetCurrencySafe(transportObject.PaymentCurrency),
            AssetFixingId = transportObject.AssetFixingId,
            AssetId = transportObject.AssetId,
            DiscountCurve = transportObject.DiscountCurve,
            FxConversionType = transportObject.FxConversionType,
            FxFixingDates = transportObject.FxFixingDates,
            FxFixingId = transportObject.FxFixingId,
            Strike = transportObject.Strike,
            Counterparty = transportObject.Counterparty,
            HedgingSet = transportObject.HedgingSet,
            PortfolioName = transportObject.PortfolioName,
            CallPut = transportObject.CallPut
        };

        private static Forward GetForward(this TO_Forward transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) => new()
        {
            TradeId = transportObject.TradeId,
            Notional = transportObject.Notional,
            Direction = transportObject.Direction,
            ExpiryDate = transportObject.ExpiryDate,
            FixingCalendar = calendarProvider.GetCalendarSafe(transportObject.FixingCalendar),
            PaymentCalendar = calendarProvider.GetCalendarSafe(transportObject.PaymentCalendar),
            SpotLag = new Frequency(transportObject.SpotLag),
            PaymentLag = new Frequency(transportObject.PaymentLag),
            Strike = transportObject.Strike,
            AssetId = transportObject.AssetId,
            PaymentCurrency = currencyProvider.GetCurrencySafe(transportObject.PaymentCurrency),
            FxFixingId = transportObject.FxFixingId,
            DiscountCurve = transportObject.DiscountCurve,
            PaymentDate = transportObject.PaymentDate,
            Counterparty = transportObject.Counterparty,
            FxConversionType = transportObject.FxConversionType,
            HedgingSet = transportObject.HedgingSet,
            PortfolioName = transportObject.PortfolioName,
        };
    }
}
