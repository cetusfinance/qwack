using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    [ProtoInclude(1, typeof(TO_IrCurve))]
    [ProtoInclude(2, typeof(TO_VolSurface))]
    [ProtoInclude(3, typeof(TO_FxMatrix))]
    public class TO_FundingModel
    {
        [ProtoMember(4)]
        public Dictionary<string, TO_IrCurve> Curves { get;  set; }
        [ProtoMember(5)]
        public Dictionary<string, TO_VolSurface> VolSurfaces { get; set; }
        [ProtoMember(6)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(7)]
        public TO_FxMatrix FxMatrix { get; set; }

    }
}
