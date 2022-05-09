using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_HazzardCurve
    {
        [ProtoMember(2)]
        public double? ConstantPD { get; set; }
        [ProtoMember(3)]
        public DateTime OriginDate { get; set; }
        [ProtoMember(4)]
        public DayCountBasis Basis { get; set; }
        [ProtoMember(5)]
        public TO_Interpolator1d HazzardCurve { get; set; }
    }
}
