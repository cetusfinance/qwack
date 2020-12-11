using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_AsianOption
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
        [ProtoMember(25)]
        public OptionType CallPut { get; set; }
    }
}
