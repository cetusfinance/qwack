using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
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
