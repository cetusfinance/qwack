using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_RiskyFlySurface : TO_GridVolSurface
    {
        [ProtoMember(5)]
        public double[][] Riskies { get; set; }
        [ProtoMember(6)]
        public double[][] Flies { get; set; }
        [ProtoMember(7)]
        public double[] ATMs { get; set; }
        [ProtoMember(8)]
        public double[] WingDeltas { get; set; }
        [ProtoMember(9)]
        public double[] Forwards { get; set; }
        [ProtoMember(10)]
        public WingQuoteType WingQuoteType { get; set; }
        [ProtoMember(11)]
        public AtmVolType AtmVolType { get; set; }

    }
}
