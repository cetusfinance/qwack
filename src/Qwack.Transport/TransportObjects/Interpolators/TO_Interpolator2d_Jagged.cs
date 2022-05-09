using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    [ProtoContract]
    public class TO_Interpolator2d_Jagged
    {
        [ProtoMember(1)]
        public double[][] Xs { get; set; }
        [ProtoMember(2)]
        public double[] Ys { get; set; }
        [ProtoMember(3)]
        public double[][] Zs { get; set; }
        [ProtoMember(4)]
        public Interpolator2DType Type { get; set; }
    }
}
