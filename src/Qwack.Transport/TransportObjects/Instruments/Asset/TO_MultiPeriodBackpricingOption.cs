using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_MultiPeriodBackpricingOption
    {
        [ProtoMember(1)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        [ProtoMember(2)]
        public OptionType CallPut { get; set; }

        [ProtoMember(3)]
        public string TradeId { get; set; }
        [ProtoMember(4)]
        public string Counterparty { get; set; }
        [ProtoMember(5)]
        public string PortfolioName { get; set; }
        [ProtoMember(6)]
        public double Notional { get; set; }
        [ProtoMember(7)]
        public TradeDirection Direction { get; set; }

        [ProtoMember(8)]
        public Tuple<DateTime, DateTime>[] PeriodDates { get; set; }
        [ProtoMember(9)]
        public DateTime DecisionDate { get; set; }
        [ProtoMember(10)]
        public DateTime SettlementDate { get; set; }
        [ProtoMember(11)]
        public List<DateArray> FixingDates { get; set; }
        [ProtoMember(12)]
        public string FixingCalendar { get; set; }
        [ProtoMember(13)]
        public string PaymentCalendar { get; set; }
        [ProtoMember(14)]
        public string SpotLag { get; set; }
        [ProtoMember(15)]
        public RollType SpotLagRollType { get; set; } = RollType.F;
        [ProtoMember(16)]
        public string PaymentLag { get; set; }
        [ProtoMember(17)]
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        [ProtoMember(18)]
        public DateTime PaymentDate { get; set; }
        [ProtoMember(19)]
        public string AssetId { get; set; }
        [ProtoMember(20)]
        public string AssetFixingId { get; set; }
        [ProtoMember(21)]
        public string FxFixingId { get; set; }
        [ProtoMember(22)]
        public List<DateArray> FxFixingDates { get; set; }
        [ProtoMember(23)]
        public string PaymentCurrency { get; set; }
        [ProtoMember(24)]
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        [ProtoMember(25)]
        public string DiscountCurve { get; set; }
        [ProtoMember(26)]
        public bool IsOption { get; set; }
    }
}
