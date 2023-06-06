using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_ITenorDate
    {
        [ProtoMember(1)]
        public DateTime? Absolute { get; set; }
        [ProtoMember(2)]
        public string Relative { get; set; }
    }
}
