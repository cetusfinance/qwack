using System.Collections.Generic;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Cubes
{
    [ProtoContract]
    public class TO_ResultCube
    {
        [ProtoMember(1)]
        public List<TO_ResultCubeRow> Rows { get; set; }
        [ProtoMember(2)]
        public Dictionary<string, string> Types { get; set; }
        [ProtoMember(3)]
        public List<string> FieldNames { get; set; }

    }
}
