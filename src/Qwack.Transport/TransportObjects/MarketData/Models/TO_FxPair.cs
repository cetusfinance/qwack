using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_FxPair
    {
        [ProtoMember(1)]
        public string Foreign { get; set; }
        [ProtoMember(2)]
        public string Domestic { get; set; }
        [ProtoMember(3)]
        public string SpotLag { get; set; }
        [ProtoMember(4)]
        public string PrimaryCalendar { get; set; }
        [ProtoMember(5)]
        public string SecondaryCalendar { get; set; }
    }
}
