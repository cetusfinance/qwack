using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_UnpricedAverage 
    {
        [ProtoMember(2)]
        public string TradeId { get; set; }
        [ProtoMember(3)]
        public string Counterparty { get; set; }
        [ProtoMember(4)]
        public string PortfolioName { get; set; }
        [ProtoMember(5)]
        public TO_AsianSwap[] PaySwaplets { get; set; }
        [ProtoMember(6)]
        public TO_AsianSwap[] RecSwaplets { get; set; }
        [ProtoMember(7)]
        public string HedgingSet { get; set; }
    }
}
