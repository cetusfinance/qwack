using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_IIrCurve
    {
        [ProtoMember(1)]
        public TO_IrCurve IrCurve { get; set; }
        [ProtoMember(2)]
        public TO_CPICurve CPICurve { get; set; }
        [ProtoMember(3)]
        public TO_SeasonalCpiCurve SeasonalCpiCurve { get; set; }
    }
}
