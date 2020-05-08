using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    [ProtoContract]
    [ProtoInclude(1,typeof(TO_Interpolator2d_Jagged))]
    [ProtoInclude(2, typeof(TO_Interpolator2d_Jagged))]
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

