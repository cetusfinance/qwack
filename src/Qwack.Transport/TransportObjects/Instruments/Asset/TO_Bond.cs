using System.Collections.Generic;
using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_Bond 
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
        public double ScalingFactor { get; set; } = 1.0;
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

        [ProtoMember(21)]
        public double? Coupon { get; set; }
        [ProtoMember(22)]
        public CouponFrequency CouponFrequency { get; set; }

        [ProtoMember(23)]
        public DateTime? IssueDate { get;set; }
        [ProtoMember(24)]
        public DateTime? MaturityDate { get; set; }
        [ProtoMember(25)]
        public DateTime? FirstCouponDate { get; set; }

        [ProtoMember(26)]
        public Dictionary<DateTime, double> CallSchedule { get; set; } 
        [ProtoMember(27)]
        public Dictionary<DateTime, double> SinkingSchedule { get; set; } 
    }
}
