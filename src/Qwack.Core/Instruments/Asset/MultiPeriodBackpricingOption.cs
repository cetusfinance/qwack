using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class MultiPeriodBackpricingOption : IHasVega, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public OptionType CallPut { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public Tuple<DateTime, DateTime>[] PeriodDates { get; set; }
        public DateTime DecisionDate { get; set; }
        public DateTime[] SettlementFixingDates { get; set; }
        public DateTime SettlementDate { get; set; }
        public List<DateTime[]> FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        public DateTime PaymentDate { get; set; }
        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }
        public string FxFixingId { get; set; }
        public string OffsetFixingId { get; set; }

        public DateShifter FixingOffset {  get; set; }

        public bool IsOption { get; set; }

        public int? DeclaredPeriod { get; set; }

        public List<DateTime[]> FxFixingDates { get; set; }
        public Currency PaymentCurrency { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        public string DiscountCurve { get; set; }

        public string[] AssetIds => new[] { AssetId };
        public string[] IrCurves(IAssetFxModel model)
        {
            if (FxConversionType == FxConversionType.None)
                return new[] { DiscountCurve };
            else
            {
                var fxCurve = model.FundingModel.FxMatrix.DiscountCurveMap[PaymentCurrency];
                var assetCurveCcy = model.GetPriceCurve(AssetId).Currency;
                var assetCurve = model.FundingModel.FxMatrix.DiscountCurveMap[assetCurveCcy];
                return (new[] { DiscountCurve, fxCurve, assetCurve }).Distinct().ToArray();
            }
        }
        public Currency Currency => PaymentCurrency;

        public DateTime LastSensitivityDate
        {
            get
            {
                if (FixingOffset != null)
                    return PaymentDate.Max(PeriodDates.Max(x => x.Item2).AddPeriod(FixingOffset.RollType, FixingOffset.Calendar, FixingOffset.Period));
                else
                    return PaymentDate.Max(PeriodDates.Max(x => x.Item2).AddPeriod(SpotLagRollType, FixingCalendar, SpotLag));
            }
        }

        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";
        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= FixingDates.Min(x => x.Min()) ?
           new Dictionary<string, List<DateTime>>() :
           new Dictionary<string, List<DateTime>> { { AssetId, FixingDates.SelectMany(x => x).Where(d => d < valDate).ToList() } };


        public IAssetInstrument Clone() => new MultiPeriodBackpricingOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            PeriodDates = PeriodDates,
            DecisionDate = DecisionDate,
            FixingDates = FixingDates.Select(x => (DateTime[])x.Clone()).ToList(),
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            SpotLagRollType = SpotLagRollType,
            PaymentLag = PaymentLag,
            PaymentLagRollType = PaymentLagRollType,
            PaymentDate = PaymentDate,
            PaymentCurrency = PaymentCurrency,
            AssetFixingId = AssetFixingId,
            AssetId = AssetId,
            DiscountCurve = DiscountCurve,
            FxConversionType = FxConversionType,
            FxFixingDates = FxFixingDates?.Select(x => (DateTime[])x.Clone()).ToList(),
            FxFixingId = FxFixingId,
            CallPut = CallPut,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            SettlementDate = SettlementDate,
            SettlementFixingDates = SettlementFixingDates,
            IsOption = IsOption,
            DeclaredPeriod = DeclaredPeriod,
            FixingOffset = FixingOffset,
            OffsetFixingId = OffsetFixingId,
        };

        public IAssetInstrument SetStrike(double strike) => throw new InvalidOperationException();

        public void FromTransportObject(TO_Instrument to_instrument, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            if(to_instrument.AssetInstrumentType != AssetInstrumentType.MultiPeriodBackpricingOption)
            {
                throw new InvalidOperationException("Incorrect instrument type");
            }
            var to = to_instrument.BackpricingOption ?? throw new InvalidOperationException("Missing backpricing option details");

            TradeId = to.TradeId;
            Notional = to.Notional;
            Direction = to.Direction;
            PeriodDates = to.PeriodDates;
            DecisionDate = to.DecisionDate;
            FixingDates = to.FixingDates.Select(fx => fx.Dates).ToList();
            FixingCalendar = to.FixingCalendar == null ? null : calendarProvider.GetCalendar(to.FixingCalendar);
            PaymentCalendar = to.PaymentCalendar == null ? null : calendarProvider.GetCalendar(to.PaymentCalendar);
            SpotLag = new Frequency(to.SpotLag);
            SpotLagRollType = to.SpotLagRollType;
            PaymentLag = new Frequency(to.PaymentLag);
            PaymentLagRollType = to.PaymentLagRollType;
            PaymentDate = to.PaymentDate;
            PaymentCurrency = currencyProvider.GetCurrency(to.PaymentCurrency);
            AssetFixingId = to.AssetFixingId;
            AssetId = to.AssetId;
            DiscountCurve = to.DiscountCurve;
            FxConversionType = to.FxConversionType;
            FxFixingDates = to.FxFixingDates?.Select(fx => fx.Dates).ToList();
            FxFixingId = to.FxFixingId;
            CallPut = to.CallPut;
            Counterparty = to.Counterparty;
            PortfolioName = to.PortfolioName;
            SettlementDate = to.SettlementDate;
            SettlementFixingDates = to.SettlementFixingDates;
            IsOption = to.IsOption;
            DeclaredPeriod = to.DeclaredPeriod;
            FixingOffset = new DateShifter(to.FixingOffset, calendarProvider);
            OffsetFixingId = to.OffsetFixingId;
        }

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.MultiPeriodBackpricingOption,
            BackpricingOption = new TO_MultiPeriodBackpricingOption
            {
                TradeId = TradeId,
                Notional = Notional,
                Direction = Direction,
                PeriodDates = PeriodDates,
                DecisionDate = DecisionDate,
                FixingDates = FixingDates?.Select(x => new DateArray((DateTime[])x.Clone()))?.ToList(),
                FixingCalendar = FixingCalendar?.Name,
                PaymentCalendar = PaymentCalendar?.Name,
                SpotLag = SpotLag.ToString(),
                SpotLagRollType = SpotLagRollType,
                PaymentLag = PaymentLag.ToString(),
                PaymentLagRollType = PaymentLagRollType,
                PaymentDate = PaymentDate,
                PaymentCurrency = PaymentCurrency,
                AssetFixingId = AssetFixingId,
                AssetId = AssetId,
                DiscountCurve = DiscountCurve,
                FxConversionType = FxConversionType,
                FxFixingDates = FxFixingDates?.Select(x => new DateArray((DateTime[])x.Clone()))?.ToList(),
                FxFixingId = FxFixingId,
                CallPut = CallPut,
                Counterparty = Counterparty,
                PortfolioName = PortfolioName,
                SettlementDate = SettlementDate,
                SettlementFixingDates = SettlementFixingDates,
                IsOption = IsOption,
                DeclaredPeriod = DeclaredPeriod,
                FixingOffset = FixingOffset.GetTransportObject(),
                OffsetFixingId = OffsetFixingId,
            }
        };
    }
}
