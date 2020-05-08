using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    [ProtoInclude(1,typeof(TO_ConstantVolSurface))]
    [ProtoInclude(2, typeof(TO_GridVolSurface))]
    [ProtoInclude(3, typeof(TO_RiskyFlySurface))]
    public class TO_VolSurface
    {
        [ProtoMember(4)]
        public TO_ConstantVolSurface ConstantVolSurface { get; set; }
        [ProtoMember(5)]
        public TO_GridVolSurface GridVolSurface { get; set; }
        [ProtoMember(6)]
        public TO_RiskyFlySurface RiskyFlySurface { get; set; }
    }
}

