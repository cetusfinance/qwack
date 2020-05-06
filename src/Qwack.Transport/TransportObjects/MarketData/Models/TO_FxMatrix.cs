using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    public class TO_FxMatrix
    {
        public string BaseCurrency { get;  set; }
        public DateTime BuildDate { get; set; }
        public List<TO_FxPair> FxPairDefinitions { get;  set; }
        public Dictionary<string, string> DiscountCurveMap { get;  set; }
        public Dictionary<string, double> SpotRates { get;  set; }
    }
}
