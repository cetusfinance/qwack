using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_GridVolSurface
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
    }
}
