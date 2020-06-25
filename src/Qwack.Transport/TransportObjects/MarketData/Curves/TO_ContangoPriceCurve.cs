using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_ContangoPriceCurve
    {
        [ProtoMember(1)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string AssetId { get; set; }
        [ProtoMember(4)]
        public string SpotLag { get; set; }
        [ProtoMember(5)]
        public string SpotCalendar { get; set; }
        [ProtoMember(6)]
        public double Spot { get; set; }
        [ProtoMember(7)]
        public DateTime SpotDate { get; set; }
        [ProtoMember(8)]
        public double[] Contangos { get; set; }
        [ProtoMember(9)]
        public DayCountBasis Basis { get; set; }
        [ProtoMember(10)]
        public string[] PillarLabels { get; set; }
        [ProtoMember(11)]
        public DateTime[] PillarDates { get; set; }
        [ProtoMember(12)]
        public string Currency { get; set; }
        [ProtoMember(13)]
        public CommodityUnits Units { get; set; }
    }
}
