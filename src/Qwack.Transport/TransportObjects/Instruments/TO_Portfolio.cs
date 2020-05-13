using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments
{
    [ProtoContract]
    public class TO_Portfolio
    {
        [ProtoMember(2)]
        public List<TO_Instrument> Instruments { get; set; }
        [ProtoMember(3)]
        public string PortfolioName { get; set; }
    }
}
