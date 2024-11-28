using System.Collections.Generic;
using ProtoBuf;

namespace Qwack.Transport.BasicTypes
{
    [ProtoContract]
    public class ProvisionRecord
    {
        [ProtoMember(1)]
        public string TradeId { get; set; }
        [ProtoMember(2)]
        public double Provision { get; set; }
        [ProtoMember(3)]
        public Dictionary<string,string> MetaData { get; set; }
    }
}
