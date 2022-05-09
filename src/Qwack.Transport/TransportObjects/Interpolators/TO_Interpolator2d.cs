using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    [ProtoContract]
    public class TO_Interpolator2d
    {
        [ProtoMember(3)]
        public TO_Interpolator2d_Jagged Jagged { get; set; }
        [ProtoMember(4)]
        public TO_Interpolator2d_Square Square { get; set; }
        [ProtoMember(5)]
        public bool IsJagged { get; set; }
    }
}

