using System;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_VolSurface_Base
    {
        [ProtoMember(1)]
        public DateTime OriginDate { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string Currency { get; set; }
        [ProtoMember(4)]
        public string AssetId { get; set; }
    }
}
