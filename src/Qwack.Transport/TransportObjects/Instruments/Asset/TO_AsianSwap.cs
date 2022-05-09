using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_AsianSwap
    {
        [ProtoMember(1)]
        public string TradeId { get; set; }
        [ProtoMember(2)]
        public string Counterparty { get; set; }
        [ProtoMember(3)]
        public string PortfolioName { get; set; }
        [ProtoMember(4)]
        public double Notional { get; set; }
        [ProtoMember(5)]
        public TradeDirection Direction { get; set; }
        [ProtoMember(6)]
        public DateTime AverageStartDate { get; set; }
        [ProtoMember(7)]
        public DateTime AverageEndDate { get; set; }
        [ProtoMember(8)]
        public DateTime[] FixingDates { get; set; }
        [ProtoMember(9)]
        public string FixingCalendar { get; set; }
        [ProtoMember(10)]
        public string PaymentCalendar { get; set; }
        [ProtoMember(11)]
        public string SpotLag { get; set; }
        [ProtoMember(12)]
        public RollType SpotLagRollType { get; set; } = RollType.F;
        [ProtoMember(13)]
        public string PaymentLag { get; set; }
        [ProtoMember(14)]
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        [ProtoMember(15)]
        public DateTime PaymentDate { get; set; }
        [ProtoMember(16)]
        public double Strike { get; set; }
        [ProtoMember(17)]
        public string AssetId { get; set; }
        [ProtoMember(18)]
        public string AssetFixingId { get; set; }
        [ProtoMember(19)]
        public string FxFixingId { get; set; }
        [ProtoMember(20)]
        public DateTime[] FxFixingDates { get; set; }
        [ProtoMember(21)]
        public string PaymentCurrency { get; set; }
        [ProtoMember(22)]
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        [ProtoMember(23)]
        public string DiscountCurve { get; set; }
        [ProtoMember(24)]
        public string HedgingSet { get; set; }




        public override bool Equals(object obj) => obj is TO_AsianSwap swap &&
                   TradeId == swap.TradeId &&
                   Counterparty == swap.Counterparty &&
                   PortfolioName == swap.PortfolioName &&
                   Notional == swap.Notional &&
                   Direction == swap.Direction &&
                   AverageStartDate == swap.AverageStartDate &&
                   AverageEndDate == swap.AverageEndDate &&
                   EqualityComparer<DateTime[]>.Default.Equals(FixingDates, swap.FixingDates) &&
                   FixingCalendar == swap.FixingCalendar &&
                   PaymentCalendar == swap.PaymentCalendar &&
                   SpotLag == swap.SpotLag &&
                   SpotLagRollType == swap.SpotLagRollType &&
                   PaymentLag == swap.PaymentLag &&
                   PaymentLagRollType == swap.PaymentLagRollType &&
                   PaymentDate == swap.PaymentDate &&
                   Strike == swap.Strike &&
                   AssetId == swap.AssetId &&
                   AssetFixingId == swap.AssetFixingId &&
                   FxFixingId == swap.FxFixingId &&
                   EqualityComparer<DateTime[]>.Default.Equals(FxFixingDates, swap.FxFixingDates) &&
                   PaymentCurrency == swap.PaymentCurrency &&
                   FxConversionType == swap.FxConversionType &&
                   DiscountCurve == swap.DiscountCurve &&
                   HedgingSet == swap.HedgingSet;

        public override int GetHashCode()
        {
            var hashCode = -696400991;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TradeId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Counterparty);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PortfolioName);
            hashCode = hashCode * -1521134295 + Notional.GetHashCode();
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + AverageStartDate.GetHashCode();
            hashCode = hashCode * -1521134295 + AverageEndDate.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime[]>.Default.GetHashCode(FixingDates);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FixingCalendar);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PaymentCalendar);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SpotLag);
            hashCode = hashCode * -1521134295 + SpotLagRollType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PaymentLag);
            hashCode = hashCode * -1521134295 + PaymentLagRollType.GetHashCode();
            hashCode = hashCode * -1521134295 + PaymentDate.GetHashCode();
            hashCode = hashCode * -1521134295 + Strike.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetFixingId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FxFixingId);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime[]>.Default.GetHashCode(FxFixingDates);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PaymentCurrency);
            hashCode = hashCode * -1521134295 + FxConversionType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DiscountCurve);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HedgingSet);
            return hashCode;
        }
    }
}
