using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    [ProtoInclude(10, typeof(TO_BasicPriceCurve))]
    [ProtoInclude(11, typeof(TO_ContangoPriceCurve))]
    [ProtoInclude(12, typeof(TO_BasisPriceCurve))]
    [ProtoInclude(13, typeof(TO_ConstantPriceCurve))]
    public class TO_PriceCurve
    {
        [ProtoMember(1)]
        public TO_BasicPriceCurve BasicPriceCurve { get; set; }
        [ProtoMember(2)]
        public TO_ContangoPriceCurve ContangoPriceCurve { get; set; }
        [ProtoMember(3)]
        public TO_BasisPriceCurve BasisPriceCurve { get; set; }
        [ProtoMember(4)]
        public TO_ConstantPriceCurve ConstantPriceCurve { get; set; }
    }
}
