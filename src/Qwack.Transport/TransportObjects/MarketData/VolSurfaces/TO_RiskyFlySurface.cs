using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_RiskyFlySurface
    {
        [ProtoMember(1)]
        public DateTime OriginDate { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string Currency { get; set; }
        [ProtoMember(4)]
        public string AssetId { get; set; }
        [ProtoMember(5)]
        public string OverrideSpotLag { get; set; }
        [ProtoMember(6)]
        public double[] Strikes { get; set; }
        [ProtoMember(7)]
        public StrikeType StrikeType { get; set; }
        [ProtoMember(8)]
        public Interpolator1DType StrikeInterpolatorType { get; set; }
        [ProtoMember(9)]
        public Interpolator1DType TimeInterpolatorType { get; set; }
        [ProtoMember(10)]
        public MultiDimArray<double> Volatilities { get; set; }
        [ProtoMember(11)]
        public DateTime[] Expiries { get; set; }
        [ProtoMember(12)]
        public string[] PillarLabels { get; set; }
        [ProtoMember(13)]
        public DayCountBasis TimeBasis { get; set; }
        [ProtoMember(14)]
        public bool FlatDeltaSmileInExtreme { get; set; }
        [ProtoMember(15)]
        public double FlatDeltaPoint { get; set; }
        [ProtoMember(16)]
        public MultiDimArray<double> Riskies { get; set; }
        [ProtoMember(17)]
        public MultiDimArray<double> Flies { get; set; }
        [ProtoMember(18)]
        public double[] ATMs { get; set; }
        [ProtoMember(19)]
        public double[] WingDeltas { get; set; }
        [ProtoMember(20)]
        public double[] Forwards { get; set; }
        [ProtoMember(21)]
        public WingQuoteType WingQuoteType { get; set; }
        [ProtoMember(22)]
        public AtmVolType AtmVolType { get; set; }

    }
}
