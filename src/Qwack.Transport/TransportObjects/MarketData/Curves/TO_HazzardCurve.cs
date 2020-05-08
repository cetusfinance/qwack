using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    [ProtoInclude(1,typeof(TO_Interpolator1d))]
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
