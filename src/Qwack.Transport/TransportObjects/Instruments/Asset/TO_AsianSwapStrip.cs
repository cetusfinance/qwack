using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    [ProtoInclude(1,typeof(TO_AsianSwap))]
    public class TO_AsianSwapStrip
    {
        [ProtoMember(2)]
        public string TradeId { get; set; }
        [ProtoMember(3)]
        public string Counterparty { get; set; }
        [ProtoMember(4)]
        public string PortfolioName { get; set; }
        [ProtoMember(5)]
        public TO_AsianSwap[] Swaplets { get; set; }
        [ProtoMember(6)]
        public string HedgingSet { get; set; }
    }
}
