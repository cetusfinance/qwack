using System.Collections.Generic;
using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_Bond : TO_CashAsset
    {
        public double? Coupon { get; set; }
        public CouponFrequency CouponFrequency { get; set; }

        public DateTime? IssueDate { get;set; }
        public DateTime? MaturityDate { get; set; }
        public DateTime? FirstCouponDate { get; set; }

        public Dictionary<DateTime, double> CallSchedule { get; set; } 
        public Dictionary<DateTime, double> SinkingSchedule { get; set; } 
    }
}
