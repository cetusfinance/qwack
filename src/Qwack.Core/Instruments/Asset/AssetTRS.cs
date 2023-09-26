using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class AssetTrs : IAssetInstrument
    {
        public AssetTrs() { }

        public AssetTrs(ITrsUnderlying underlying, TrsLegType assetLegResetType, SwapLegType fundingLegType, TrsLegType fundingLegResetType, FloatRateIndex rateIndex, 
            double notional, double fundingFixedRateOrMargin, FxConversionType fxConversionType, DateTime startDate, DateTime endDate, Currency currency )
        {
            Underlying = underlying;
            AssetLegResetType = assetLegResetType;
            FundingLegType = fundingLegType;
            FundingLegResetType = fundingLegResetType;
            RateIndex = rateIndex;
            Notional = notional;
            FundingFixedRateOrMargin = fundingFixedRateOrMargin;
            FxConversionType = fxConversionType;
            StartDate = startDate;
            EndDate = endDate;
            Currency = currency;

            /*
             *             FixedLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFixed)
            {
                FixedRateOrMargin = (decimal)parRate,
                LegType = SwapLegType.Fixed,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? -1.0M : 1.0M),
                AccrualDCB = rateIndex.DayCountBasisFixed
            };
            FloatLeg = new GenericSwapLeg(StartDate, swapTenor, rateIndex.HolidayCalendars, rateIndex.Currency,
                ResetFrequency, BasisFloat)
            {
                FixedRateOrMargin = 0.0M,
                LegType = SwapLegType.Float,
                Nominal = 1e6M * (swapType == SwapPayReceiveType.Payer ? 1.0M : -1.0M),
                AccrualDCB = rateIndex.DayCountBasis
            };
            FlowScheduleFixed = FixedLeg.GenerateSchedule();
            FlowScheduleFloat = FloatLeg.GenerateSchedule();
            */
        }

        public ITrsUnderlying Underlying { get; set; }

        public TrsLegType AssetLegResetType { get; set; } = TrsLegType.Bullet;
        public TrsLegType FundingLegResetType { get; set; } = TrsLegType.Resetting;
        public SwapLegType FundingLegType { get; set; } = SwapLegType.Float;
        public FxConversionType FxConversionType { get; set; }

        public string[] AssetIds => Underlying.AssetIds;

        public Currency PaymentCurrency => Currency;

        public FloatRateIndex RateIndex { get; set; }

        public GenericSwapLeg FundingLeg { get; set; }
        public CashFlowSchedule FlowScheduleFunding { get; set; }

        public GenericSwapLeg AssetLeg { get; set; }
        public CashFlowSchedule FlowScheduleAsset { get; set; }

        public string TradeId { get; set; }        
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }

        public double Notional { get; set; }

        public double FundingFixedRateOrMargin { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public Currency Currency { get; set; }

        public string ForecastFundingCurve { get; set; }

        public string DiscountCurve { get; set; }

        public Dictionary<string, string> MetaData { get; set; }

        public double PV(IAssetFxModel model)
        {
            var fundingLegPv = FlowScheduleFunding.PV(Currency, model, null);
            var assetLegPv = 0;

            return fundingLegPv + assetLegPv;
        }

        public IAssetInstrument Clone() => throw new NotImplementedException();
        public string FxPair(IAssetFxModel model) => throw new NotImplementedException();
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType;
        public string[] IrCurves(IAssetFxModel model) => new[] { ForecastFundingCurve, DiscountCurve };
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => throw new NotImplementedException();
        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.AssetTrs,
            AssetTrs = new TO_AssetTrs()
            {
                AssetLeg = AssetLeg.GetTransportObject(),
                AssetLegResetType = AssetLegResetType,
                Counterparty = Counterparty,
                Currency = Currency.Ccy,
                DiscountCurve = DiscountCurve,
                EndDate = EndDate,
                FlowScheduleAsset = FlowScheduleAsset.GetTransportObject(),
                FlowScheduleFunding = FlowScheduleFunding.GetTransportObject(),
                ForecastFundingCurve = ForecastFundingCurve,
                FundingFixedRateOrMargin = FundingFixedRateOrMargin,
                FundingLeg = FundingLeg.GetTransportObject(),
                FundingLegResetType = FundingLegResetType,
                FundingLegType = FundingLegType,
                FxConversionType = FxConversionType,
                MetaData = new(MetaData),
                Notional = Notional,
                PortfolioName = PortfolioName,
                RateIndex = RateIndex.GetTransportObject(),
                StartDate = StartDate,
                TradeId = TradeId,
                Underlying = Underlying.ToTransportObject()
            }
        };
    }
}
