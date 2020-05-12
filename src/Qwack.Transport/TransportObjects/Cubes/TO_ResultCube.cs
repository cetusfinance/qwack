using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Cubes
{
    [ProtoContract]
    [ProtoInclude(10,typeof(TO_ResultCubeRow))]
    public class TO_ResultCube
    {
        [ProtoMember(1)]
        public List<TO_ResultCubeRow> Rows { get; set; }
        [ProtoMember(2)]
        public Dictionary<string, string> Types { get; set; }
        [ProtoMember(3)]
        public List<string> FieldNames { get; set; }

    }

    [ProtoContract]
    public class TO_ResultCubeRow
    {
        [ProtoMember(1)]
        public double Value { get; set; }
        [ProtoMember(2)]
        public string[] MetaData { get; set; }
    }
}
