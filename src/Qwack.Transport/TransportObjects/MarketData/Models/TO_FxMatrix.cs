using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_FxMatrix
    {
        [ProtoMember(2)]
        public string BaseCurrency { get; set; }
        [ProtoMember(3)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(4)]
        public List<TO_FxPair> FxPairDefinitions { get; set; }
        [ProtoMember(5)]
        public Dictionary<string, string> DiscountCurveMap { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, double> SpotRates { get; set; }
    }
}
