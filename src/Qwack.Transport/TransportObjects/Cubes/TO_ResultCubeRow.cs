using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Cubes
{
    [ProtoContract]
    public class TO_ResultCubeRow
    {
        [ProtoMember(1)]
        public double Value { get; set; }
        [ProtoMember(2)]
        public string[] MetaData { get; set; }
    }
}
