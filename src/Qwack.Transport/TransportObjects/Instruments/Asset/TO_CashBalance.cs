using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_CashBalance
    {
        [ProtoMember(1)]
        public double Notional { get; set; }
        [ProtoMember(2)]
        public string Currency { get; set; }
        [ProtoMember(3)]
        public DateTime? PayDate { get; set; }
        [ProtoMember(4)]
        public string SolveCurve { get; set; }
        [ProtoMember(5)]
        public string TradeId { get; set; }
        [ProtoMember(6)]
        public string Counterparty { get; set; }
        [ProtoMember(7)]
        public DateTime PillarDate { get; set; }

    }
}
