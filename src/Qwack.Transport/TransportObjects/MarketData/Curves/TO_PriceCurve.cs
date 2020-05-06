using System;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves 
{
    public class TO_PriceCurve
    {
        public PriceCurveType CurveType { get; set; }
        public DateTime BuildDate { get; set; }
        public string Name { get; set; }
        public string AssetId { get; set; }
        public string SpotLag { get; set; } 
        public string SpotCalendar { get; set; }
        public DateTime[] PillarDates { get; set; }
        public double[] Prices { get; set; }
        public string[] PillarLabels { get; set; }
        public string Currency { get; set; }
        public string CollateralSpec { get; set; }
    }

}
