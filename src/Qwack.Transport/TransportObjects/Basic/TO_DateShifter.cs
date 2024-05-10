
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Basic
{
    [ProtoContract]
    public class TO_DateShifter
    {
        [ProtoMember(1)]
        public string Period { get; set; }
        [ProtoMember(2)]
        public RollType RollType { get; set; }
        [ProtoMember(3)]
        public string Calendar { get; set; }
    }
}
