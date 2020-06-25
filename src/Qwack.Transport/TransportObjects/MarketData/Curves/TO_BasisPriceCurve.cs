using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_BasisPriceCurve
    {
        public TO_PriceCurve BaseCurve { get; set; }
        public TO_PriceCurve Curve { get; set; }
        public DateTime BuildDate { get; set; }
        public string Name { get; set; }
        public string AssetId { get; set; }
        public string Currency { get; set; }
        public PriceCurveType CurveType { get; set; }
        public List<TO_Instrument> Instruments { get; set; }
        public List<DateTime> Pillars { get; set; }
        public List<string> PillarLabels { get; set; }
        public TO_IrCurve DiscountCurve { get; set; }
        public string SpotLag { get; set; }
        public string SpotCalendar { get; set; }
        public CommodityUnits Units { get; set; }

    }
}
