using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_AsianOption : TO_AsianSwap
    {
        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
    }
}
