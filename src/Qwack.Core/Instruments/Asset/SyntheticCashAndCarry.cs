using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class SyntheticCashAndCarry : IAssetInstrument
    {
        public SyntheticCashAndCarry() { }

        public SyntheticCashAndCarry(TO_SyntheticCashAndCarry to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            TradeId = to.TradeId;
            Counterparty = to.Counterparty;
            PortfolioName = to.PortfolioName;
            HedgingSet = to.HedgingSet;
            DiscountCurve = to.DiscountCurve;
            SpotLag = string.IsNullOrEmpty(to.SpotLag) ? 0.Bd() : new Frequency(to.SpotLag);
            FundingBasis = to.FundingBasis;
            FundingRate = to.FundingRate;
            FundingRateType = to.FundingRateType;
            MetaData = to.MetaData==null ?  new() : new(to.MetaData);
            NearLeg = to.NearLeg.GetForward(currencyProvider, calendarProvider);
            FarLeg = to.FarLeg.GetForward(currencyProvider, calendarProvider);
        }

        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public string HedgingSet { get; set; }

        public Forward NearLeg { get; set; }
        public Forward FarLeg { get; set; }

        public string[] AssetIds => (new[] { NearLeg.AssetId, FarLeg.AssetId }).Distinct().ToArray();
        public string[] IrCurves(IAssetFxModel model) => new[] { DiscountCurve };
        public Currency Currency => NearLeg.PaymentCurrency;
        public Currency PaymentCurrency => Currency;
        public DateTime LastSensitivityDate => FarLeg.LastSensitivityDate;

        public DayCountBasis FundingBasis {  get; set; }
        public double FundingRate { get; set; }
        public RateType FundingRateType { get; set; } = RateType.Linear;
        public string DiscountCurve { get; set; }
        public Frequency SpotLag { get; set; } = 0.Bd();

        public double Notional => NearLeg.Notional;

        public IAssetInstrument Clone() => new SyntheticCashAndCarry
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            DiscountCurve = DiscountCurve,
            SpotLag = SpotLag,
            FundingBasis = FundingBasis,
            FundingRate = FundingRate,
            FundingRateType = FundingRateType,
            MetaData = MetaData,
            NearLeg = NearLeg,
            FarLeg = FarLeg,
        };

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public double Strike => FundingRate;

        public FxConversionType FxType(IAssetFxModel model) => NearLeg.FxType(model);
        public string FxPair(IAssetFxModel model) => NearLeg.FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            NearLeg.PastFixingDates(valDate)
            .Concat(FarLeg.PastFixingDates(valDate))
            .Distinct()
            .ToDictionary(x => x.Key, x => x.Value);

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
        {
            var payDate = FarLeg.PaymentDate == DateTime.MinValue ?
              FarLeg.ExpiryDate.AddPeriod(RollType.F, FarLeg.PaymentCalendar, FarLeg.PaymentLag) :
              FarLeg.PaymentDate;

            var effectiveDate = NearLeg.PaymentDate == DateTime.MinValue ?
            NearLeg.ExpiryDate.AddPeriod(RollType.F, NearLeg.PaymentCalendar, NearLeg.PaymentLag) :
            NearLeg.PaymentDate;

            var spreadInitial = FarLeg.Strike - NearLeg.Strike;

            var t = effectiveDate.CalculateYearFraction(payDate, FundingBasis);
            var accruedFraction = 1.0 / IrCurve.DFFromRate(t, FundingRate, FundingRateType) - 1.0;
            var fundingNominal = NearLeg.Strike * NearLeg.Notional;
            var fundingPv = fundingNominal * accruedFraction;

            var spreadLegFv = spreadInitial * NearLeg.Notional;

            var f = new List<CashFlow>
            {
                new CashFlow
                {
                    Notional = spreadLegFv,
                    SettleDate = payDate,
                    FlowType = FlowType.FixedRate,
                    FixedRateOrMargin = FundingRate
                },

                new CashFlow
                {
                    Notional = -fundingPv,
                    SettleDate = payDate, 
                    FlowType = FlowType.FixedRate 
                }
            };

            return f;
        }
            

        public TO_Instrument ToTransportObject() =>
           new()
           {
               AssetInstrumentType = AssetInstrumentType.SyntheticCashAndCarry,
               SyntheticCashAndCarry = new TO_SyntheticCashAndCarry
               {
                   TradeId = TradeId,
                   Counterparty = Counterparty,
                   PortfolioName = PortfolioName,
                   HedgingSet = HedgingSet,
                   DiscountCurve = DiscountCurve,
                   SpotLag = SpotLag.ToString(),
                   FundingBasis = FundingBasis,
                   FundingRate = FundingRate,
                   FundingRateType = FundingRateType,
                   MetaData = MetaData,
                   NearLeg = NearLeg.ToTransportObject().Forward,
                   FarLeg = FarLeg.ToTransportObject().Forward,
               }
           };
    }
}
