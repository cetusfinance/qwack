using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Transport.TransportObjects.Instruments
{
    [ProtoContract]
    public class TO_Instrument
    {
        [ProtoMember(100)]
        public FundingInstrumentType FundingInstrumentType { get; set; } 
        [ProtoMember(101)]
        public AssetInstrumentType AssetInstrumentType { get; set; }
        [ProtoMember(102)]
        public TO_AsianSwap AsianSwap { get; set; }
        [ProtoMember(103)]
        public TO_AsianSwapStrip AsianSwapStrip { get; set; }
        [ProtoMember(104)]
        public TO_AsianOption AsianOption { get; set; }
        [ProtoMember(105)]
        public TO_Forward Forward { get; set; }
        [ProtoMember(106)]
        public TO_EuropeanOption EuropeanOption { get; set; }
    }
}
