using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_IrCurve
    {
        [ProtoMember(1)]
        public DateTime[] Pillars { get; set; }
        [ProtoMember(2)]
        public double[] Rates { get; set; }
        [ProtoMember(3)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(4)]
        public string Name { get; set; }
        [ProtoMember(5)]
        public Interpolator1DType InterpKind { get; set; }
        [ProtoMember(6)]
        public string Ccy { get; set; }
        [ProtoMember(7)]
        public string CollateralSpec { get; set; }
        [ProtoMember(8)]
        public RateType RateStorageType { get; set; } = RateType.CC;
        [ProtoMember(9)]
        public DayCountBasis Basis { get; set; } = DayCountBasis.ACT365F;
    }
}
