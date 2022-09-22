using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_CashWrapper
    {
        [ProtoMember(1)]
        public TO_Instrument Underlying { get; set; }
        [ProtoMember(2)]
        public TO_CashBalance[] CashBalances { get; set; }

    }
}
