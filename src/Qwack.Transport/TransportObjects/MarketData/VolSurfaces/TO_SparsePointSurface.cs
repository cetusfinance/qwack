using System.Collections.Generic;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_SparsePointSurface : TO_VolSurface_Base
    {
        [ProtoMember(5)]
        public Dictionary<string, double> Vols { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, string> PointLabels { get; set; }
    }
}
