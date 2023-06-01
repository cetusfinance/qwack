using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_FundingModel
    {
        [ProtoMember(4)]
        public Dictionary<string, TO_IIrCurve> Curves { get; set; }
        [ProtoMember(5)]
        public Dictionary<string, TO_VolSurface> VolSurfaces { get; set; }
        [ProtoMember(6)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(7)]
        public TO_FxMatrix FxMatrix { get; set; }
        [ProtoMember(8)]
        public Dictionary<string, TO_FixingDictionary> Fixings { get; set; }

    }
}
