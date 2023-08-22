using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_CPICurve 
    {
        [ProtoMember(1)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(2)]
        public DayCountBasis Basis { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        [ProtoMember(4)]
        public DateTime[] PillarDates { get; set; } = Array.Empty<DateTime>();
        [ProtoMember(5)]
        public double[] CpiRates { get; set; } = Array.Empty<double>();
        [ProtoMember(6)]
        public TO_InflationIndex InflationIndex { get; set; }
        [ProtoMember(7)]
        public int SolveStage { get; set; }
        [ProtoMember(8)]
        public string CollateralSpec { get; set; }
        [ProtoMember(9)]
        public CpiInterpolationType CpiInterpolationType { get; set; }
        [ProtoMember(10)]
        public double SpotFixing { get; set; }
        [ProtoMember(11)]
        public DateTime SpotDate { get; set; }
        [ProtoMember(12)]
        public Dictionary<DateTime, double> Fixings { get; set; }

    }
}
