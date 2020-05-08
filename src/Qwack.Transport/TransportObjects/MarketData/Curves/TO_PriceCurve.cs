using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves 
{
    [ProtoContract]
    public class TO_PriceCurve
    {
        [ProtoMember(1)]
        public PriceCurveType CurveType { get; set; }
        [ProtoMember(2)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        [ProtoMember(4)]
        public string AssetId { get; set; }
        [ProtoMember(5)]
        public string SpotLag { get; set; }
        [ProtoMember(6)]
        public string SpotCalendar { get; set; }
        [ProtoMember(7)]
        public DateTime[] PillarDates { get; set; }
        [ProtoMember(8)]
        public double[] Prices { get; set; }
        [ProtoMember(9)]
        public string[] PillarLabels { get; set; }
        [ProtoMember(10)]
        public string Currency { get; set; }
        [ProtoMember(11)]
        public string CollateralSpec { get; set; }
    }

}
