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
    }
}
