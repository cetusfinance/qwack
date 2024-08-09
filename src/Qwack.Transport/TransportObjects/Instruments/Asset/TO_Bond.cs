using System.Collections.Generic;
using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_Bond : TO_CashAsset
    {
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
