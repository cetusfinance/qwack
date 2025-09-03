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
    public class StripQPSwaption : IHasVega, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public OptionType CallPut { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public DateTime[] QPs { get; set; }
        public DateTime DecisionDate { get; set; }
        public int[] Offsets { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        public DateTime PaymentDate { get; set; }
        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }

        public double PremiumTotal { get; set; }
        public DateTime PremiumSettleDate { get; set; }

        public Currency PaymentCurrency { get; set; }
        public string DiscountCurve { get; set; }


        public string[] AssetIds => [AssetId];
        public string[] IrCurves(IAssetFxModel model) => [DiscountCurve];
        public Currency Currency => PaymentCurrency;

        public DateTime LastSensitivityDate
        {
            get
            {
                var maxOffset = Offsets.Max();
                return PaymentDate.Max(QPs.Max().AddPeriod(RollType.F, null, maxOffset.Months()));
            }
        }

        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => [];
        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) => [];

        public IAssetInstrument Clone() => new StripQPSwaption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            DecisionDate = DecisionDate,
            QPs = (DateTime[])QPs.Clone(),
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
            CallPut = CallPut,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,        
            MetaData = new(MetaData),      
            PremiumSettleDate = PremiumSettleDate,
            PremiumTotal = PremiumTotal,
            Offsets = (int[])Offsets.Clone(),
        };

        public IAssetInstrument SetStrike(double strike) => throw new InvalidOperationException();

        public void FromTransportObject(TO_Instrument to_instrument, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            if(to_instrument.AssetInstrumentType != AssetInstrumentType.StripQPSwaption)
            {
                throw new InvalidOperationException("Incorrect instrument type");
            }
            var to = to_instrument.StripQPSwaption ?? throw new InvalidOperationException("Missing strip QP swaption details");

            TradeId = to.TradeId;
            Notional = to.Notional;
            Direction = to.Direction;
            QPs = to.QPs;
            DecisionDate = to.DecisionDate;
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
            CallPut = to.CallPut;
            Counterparty = to.Counterparty;
            PortfolioName = to.PortfolioName;
            PremiumTotal = to.PremiumTotal;
            PremiumSettleDate = to.PremiumSettleDate;
            Offsets = to.Offsets;
        }

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.StripQPSwaption,
            StripQPSwaption = new TO_StripQPSwaption
            {
                TradeId = TradeId,
                Notional = Notional,
                Direction = Direction,
                QPs = QPs,
                DecisionDate = DecisionDate,
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
                CallPut = CallPut,
                Counterparty = Counterparty,
                PortfolioName = PortfolioName,
                MetaData = new(MetaData),
                PremiumSettleDate = PremiumSettleDate,
                PremiumTotal = PremiumTotal,
                Offsets = Offsets
            }
        };
    }
}
