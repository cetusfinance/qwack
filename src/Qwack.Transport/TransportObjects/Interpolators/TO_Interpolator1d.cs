using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    [ProtoContract]
    public class TO_Interpolator1d
    {
        [ProtoMember(1)]
        public double[] Xs { get; set; }
        [ProtoMember(2)]
        public double[] Ys { get; set; }
        [ProtoMember(3)]
        public Interpolator1DType Type { get; set; }
        [ProtoMember(4)]
        public bool IsSorted { get; set; }
        [ProtoMember(5)]
        public bool NoCopy { get; set; }
    }
}
