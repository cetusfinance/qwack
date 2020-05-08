using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_EuropeanOption : TO_Forward
    {
        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
    }
}
