using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_ConstantPriceCurve
    {
        [ProtoMember(1)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(2)]
        public double Price { get; set; }
        [ProtoMember(3)]
        public string Currency { get; set; }
        [ProtoMember(4)]
        public string AssetId { get; set; }
        [ProtoMember(5)]
        public string Name { get; set; }
        [ProtoMember(6)]
        public CommodityUnits Units { get; set; }
    }
}
