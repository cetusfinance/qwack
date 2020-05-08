using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_VolSurfaceKey
    {
        [ProtoMember(1)]
        public string AssetId { get; set; }
        [ProtoMember(2)]
        public string Currency { get; set; }
    }
}
