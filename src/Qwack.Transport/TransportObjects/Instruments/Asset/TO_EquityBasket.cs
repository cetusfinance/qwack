using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_EquityBasket
    {
        [ProtoMember(1)]
        public string Name { get; set; }
        [ProtoMember(2)]
        public string Currency { get; set; }
        [ProtoMember(3)]
        public FxConversionType FxConversionType { get; set; }
        [ProtoMember(5)]
        public double Notional { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, double> Weights { get; set; }
        [ProtoMember(7)]
        public Dictionary<string, string> MetaData { get; set; }


    }
}
