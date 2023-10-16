using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class AssetTrs : IAssetInstrument
    {
        public AssetTrs() { }

        public AssetTrs(ITrsUnderlying underlying, TrsLegType assetLegResetType, SwapLegType fundingLegType, TrsLegType fundingLegResetType, FloatRateIndex rateIndex,
            double notional, double fundingFixedRateOrMargin, FxConversionType fxConversionType, DateTime startDate, DateTime endDate, Currency currency, Calendar settleCalendar,
            SwapPayReceiveType assetLegDirection = SwapPayReceiveType.Rec, string paymentOffset = "2b", double? initialFixing = null)
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
            SettleCalendar = settleCalendar;
            InitialAssetFixing = initialFixing;

            FundingLegPaymentOffset = new Frequency(paymentOffset);
            AssetLegPaymentOffset = new Frequency(paymentOffset);

            PayRecAsset = assetLegDirection;

            FundingLeg = new GenericSwapLeg(StartDate, EndDate, rateIndex.HolidayCalendars, rateIndex.Currency,
                rateIndex.ResetTenor, rateIndex.DayCountBasis)
            {
                FixedRateOrMargin = (decimal)fundingFixedRateOrMargin,
                LegType = fundingLegType,
                Nominal = (decimal)Notional * (assetLegDirection == SwapPayReceiveType.Receiver ? -1.0M : 1.0M),
                AccrualDCB = rateIndex.DayCountBasis,
                PaymentOffset = FundingLegPaymentOffset,
                PaymentCalendar = SettleCalendar
            };

            AssetLeg = new GenericSwapLeg(StartDate, EndDate, rateIndex.HolidayCalendars, rateIndex.Currency,
                rateIndex.ResetTenor, rateIndex.DayCountBasis)
            {
                FixedRateOrMargin = 0.0M,
                LegType = SwapLegType.AssetPerformance,
                TrsLegType = assetLegResetType,
                Nominal = (decimal)Notional * (assetLegDirection == SwapPayReceiveType.Receiver ? 1.0M : -1.0M),
                AccrualDCB = rateIndex.DayCountBasisFixed,
                AssetId = Underlying.AssetIds[0],
                PaymentOffset = AssetLegPaymentOffset,
                PaymentCalendar = SettleCalendar
            };

            FlowScheduleFunding = FundingLeg.GenerateSchedule();
            FlowScheduleAsset = AssetLeg.GenerateSchedule();
        }

        public AssetTrs(TO_AssetTrs to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            Underlying = TrsUnderlyingFactory.FromTo(to.Underlying, currencyProvider);
            AssetLegResetType = to.AssetLegResetType;
            FundingLegType = to.FundingLegType;
            FundingLegResetType = to.FundingLegResetType;
            RateIndex = new FloatRateIndex(to.RateIndex, calendarProvider, currencyProvider);
            Notional = to.Notional;
            FundingFixedRateOrMargin = to.FundingFixedRateOrMargin;
            FxConversionType = to.FxConversionType;
            StartDate = to.StartDate;
            EndDate = to.EndDate;
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            SettleCalendar = calendarProvider.GetCalendarSafe(to.SettleCalendar);

            FundingLeg = new GenericSwapLeg(to.FundingLeg, calendarProvider, currencyProvider);

            AssetLeg = new GenericSwapLeg(to.AssetLeg, calendarProvider, currencyProvider);

            FlowScheduleFunding = new CashFlowSchedule(to.FlowScheduleFunding, calendarProvider, currencyProvider);
            FlowScheduleAsset = new CashFlowSchedule(to.FlowScheduleAsset, calendarProvider, currencyProvider);

            InitialAssetFixing = to.InitialAssetFixing;

            FundingLegPaymentOffset = new Frequency(to.FundingLegPaymentOffset);
            AssetLegPaymentOffset = new Frequency(to.AssetLegPaymentOffset);

            DiscountCurve = to.DiscountCurve;
            ForecastFundingCurve = to.ForecastFundingCurve;
            PayRecAsset = to.PayRecAsset;
        }

        public ITrsUnderlying Underlying { get; set; }

        public TrsLegType AssetLegResetType { get; set; } = TrsLegType.Bullet;
        public TrsLegType FundingLegResetType { get; set; } = TrsLegType.Resetting;
        public SwapLegType FundingLegType { get; set; } = SwapLegType.Float;
        public FxConversionType FxConversionType { get; set; }
        public SwapPayReceiveType PayRecAsset { get; set; } = SwapPayReceiveType.Rec;

        public string[] AssetIds => Underlying.AssetIds;

        public Currency PaymentCurrency => Currency;
        public Calendar SettleCalendar { get; set; }
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

        public double? InitialAssetFixing { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Frequency AssetLegPaymentOffset { get; set; } = new Frequency("2b");
        public Frequency FundingLegPaymentOffset { get; set; } = new Frequency("2b");

        public DateTime LastSensitivityDate => EndDate;

        public Currency Currency { get; set; }

        public string ForecastFundingCurve { get; set; }

        public string DiscountCurve { get; set; }

        public Dictionary<string, string> MetaData { get; set; }

        public double PV(IAssetFxModel model)
        {
            var fundingLegPv = FlowScheduleFunding.PV(Currency, model.FundingModel, ForecastFundingCurve, RateIndex.DayCountBasis, null);
            var assetLegPv = FlowScheduleAsset.PV(Currency, model, null);

            return fundingLegPv + assetLegPv;
        }

        public IAssetInstrument Clone() => new AssetTrs
        {
            AssetLeg = AssetLeg.Clone(),
            AssetLegResetType = AssetLegResetType,
            Counterparty = Counterparty,
            Currency = Currency,
            DiscountCurve = DiscountCurve,
            EndDate = EndDate,
            FlowScheduleAsset = FlowScheduleAsset.Clone(),
            FlowScheduleFunding = FlowScheduleFunding.Clone(),
            ForecastFundingCurve = ForecastFundingCurve,
            FundingFixedRateOrMargin = FundingFixedRateOrMargin,
            FundingLeg = FundingLeg.Clone(),
            FundingLegResetType = FundingLegResetType,
            FundingLegType = FundingLegType,
            FxConversionType = FxConversionType,
            MetaData = new(MetaData),
            Notional = Notional,
            PortfolioName = PortfolioName,
            RateIndex = RateIndex,
            StartDate = StartDate,
            TradeId = TradeId,
            Underlying = Underlying,
            SettleCalendar = SettleCalendar,
            FundingLegPaymentOffset = FundingLegPaymentOffset,
            AssetLegPaymentOffset = AssetLegPaymentOffset,
            InitialAssetFixing = InitialAssetFixing,
            PayRecAsset = PayRecAsset
        };

        public string FxPair(IAssetFxModel model) => throw new NotImplementedException();
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType;
        public string[] IrCurves(IAssetFxModel model) => new[] { ForecastFundingCurve, DiscountCurve };
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate)
        {
            if (valDate > EndDate)
                return new Dictionary<string, List<DateTime>>() { { Underlying.AssetIds[0], new List<DateTime> { StartDate, EndDate } } };
            else if (valDate > StartDate)
                return new Dictionary<string, List<DateTime>>() { { Underlying.AssetIds[0], new List<DateTime> { StartDate } } };
            else
                return new Dictionary<string, List<DateTime>>() { { Underlying.AssetIds[0], new List<DateTime>() } };
        }

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public double FlowsT0(IAssetFxModel model)
        {
            var flowsAsset = FlowScheduleAsset.FlowsT0(Currency, model, model.BuildDate);
            var flowsFunding = FlowScheduleFunding.FlowsT0(Currency, model.FundingModel, ForecastFundingCurve, RateIndex.DayCountBasis, model.BuildDate);
            return flowsAsset + flowsFunding;
        }

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model)
            => FlowScheduleAsset.ExpectedFlows(Currency, model, null)
                                .Concat(FlowScheduleFunding.ExpectedFlows(Currency, model.FundingModel, ForecastFundingCurve, RateIndex.DayCountBasis, null))
                                .ToList();

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.AssetTrs,
            AssetTrs = new TO_AssetTrs()
            {
                AssetLeg = AssetLeg?.GetTransportObject(),
                AssetLegResetType = AssetLegResetType,
                Counterparty = Counterparty,
                Currency = Currency?.Ccy,
                DiscountCurve = DiscountCurve,
                EndDate = EndDate,
                FlowScheduleAsset = FlowScheduleAsset?.GetTransportObject(),
                FlowScheduleFunding = FlowScheduleFunding?.GetTransportObject(),
                ForecastFundingCurve = ForecastFundingCurve,
                FundingFixedRateOrMargin = FundingFixedRateOrMargin,
                FundingLeg = FundingLeg?.GetTransportObject(),
                FundingLegResetType = FundingLegResetType,
                FundingLegType = FundingLegType,
                FxConversionType = FxConversionType,
                MetaData = MetaData == null ? null : new(MetaData),
                Notional = Notional,
                PortfolioName = PortfolioName,
                RateIndex = RateIndex?.GetTransportObject(),
                StartDate = StartDate,
                TradeId = TradeId,
                Underlying = Underlying?.ToTransportObject(),
                AssetLegPaymentOffset = AssetLegPaymentOffset.ToString(),
                FundingLegPaymentOffset = FundingLegPaymentOffset.ToString(),
                SettleCalendar = SettleCalendar?.Name,
                InitialAssetFixing = InitialAssetFixing,
                PayRecAsset = PayRecAsset
            }
        };
    }
}
