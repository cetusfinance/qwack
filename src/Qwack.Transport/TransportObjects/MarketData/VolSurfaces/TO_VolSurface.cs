using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_VolSurface
    {
        [ProtoMember(4)]
        public TO_ConstantVolSurface ConstantVolSurface { get; set; }
        [ProtoMember(5)]
        public TO_GridVolSurface GridVolSurface { get; set; }
        [ProtoMember(6)]
        public TO_RiskyFlySurface RiskyFlySurface { get; set; }
        [ProtoMember(7)]
        public TO_SparsePointSurface SparsePointSurface { get; set; }
    }
}

