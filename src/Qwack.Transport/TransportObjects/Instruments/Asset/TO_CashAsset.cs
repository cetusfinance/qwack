using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_CashAsset
    {
        [ProtoMember(1)]
        public string TradeId { get; set; }
        [ProtoMember(2)]
        public string Currency { get; set; }
        [ProtoMember(3)]
        public string Counterparty { get; set; }
        [ProtoMember(4)]
        public string PortfolioName { get; set; }
        [ProtoMember(5)]
        public double Notional { get; set; }
        [ProtoMember(6)]
        public string AssetId { get; set; }
        [ProtoMember(7)]
        public double ScalingFactor { get; set; }
        [ProtoMember(8)]
        public string SettleLag { get; set; }
        [ProtoMember(9)]
        public string SettleCalendar { get; set; }
        [ProtoMember(10)]
        public DateTime? SettleDate { get; set; }
        [ProtoMember(11)]
        public double? Price { get; set; }
        [ProtoMember(12)]
        public Dictionary<string, string> MetaData { get; set; }

    }
}
