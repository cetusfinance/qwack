using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_ITrsUnderlying
    {
        [ProtoMember(1)]
        public TO_EquityIndex EquityIndex { get; set; }

        [ProtoMember(2)]
        public TO_EquityBasket EquityBasket { get; set; }

    }
}
