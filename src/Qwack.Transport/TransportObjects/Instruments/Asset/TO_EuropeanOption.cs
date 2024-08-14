using System.Collections.Generic;
using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_EuropeanOption
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
        public DateTime ExpiryDate { get; set; }
        [ProtoMember(7)]
        public string FixingCalendar { get; set; }
        [ProtoMember(8)]
        public string PaymentCalendar { get; set; }
        [ProtoMember(9)]
        public string SpotLag { get; set; }
        [ProtoMember(10)]
        public string PaymentLag { get; set; }
        [ProtoMember(11)]
        public DateTime PaymentDate { get; set; }
        [ProtoMember(12)]
        public double Strike { get; set; }
        [ProtoMember(13)]
        public string AssetId { get; set; }
        [ProtoMember(14)]
        public string PaymentCurrency { get; set; }
        [ProtoMember(15)]
        public string FxFixingId { get; set; }
        [ProtoMember(16)]
        public string DiscountCurve { get; set; }
        [ProtoMember(17)]
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        [ProtoMember(18)]
        public string HedgingSet { get; set; }
        [ProtoMember(19)]
        public Dictionary<string, string> MetaData { get; set; }

        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
    }
}
