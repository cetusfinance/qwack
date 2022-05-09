using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_FixingDictionary
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public string AssetId { get; set; }
        [ProtoMember(3)]
        public string FxPair { get; set; }
        [ProtoMember(4)]
        public FixingDictionaryType FixingDictionaryType { get; set; }
        [ProtoMember(5)]
        public Dictionary<string, double> Fixings { get; set; }

    }
}
