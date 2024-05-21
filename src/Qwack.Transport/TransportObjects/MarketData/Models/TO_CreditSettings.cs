using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_CreditSettings
    {
        [ProtoMember(1)]
        public BaseMetric Metric { get; set; } = BaseMetric.PV;
        [ProtoMember(2)]
        public double ConfidenceInterval { get; set; } = 0.95;
        [ProtoMember(3)]
        public TO_HazzardCurve CreditCurve { get; set; }
        [ProtoMember(4)]
        public double LGD { get; set; }
        [ProtoMember(5)]
        public double CounterpartyRiskWeighting { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, string> AssetIdToHedgeGroupMap { get; set; }
        [ProtoMember(7)]
        public TO_IIrCurve FundingCurve { get; set; }
        [ProtoMember(8)]
        public TO_IIrCurve BaseDiscountCurve { get; set; }
        [ProtoMember(9)]
        public PFERegressorType PfeRegressorType { get; set; }
        [ProtoMember(10)]
        public DateTime[] ExposureDates { get; set; }
    }
}
