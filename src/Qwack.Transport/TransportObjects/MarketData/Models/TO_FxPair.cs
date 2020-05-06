using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    public class TO_FxPair
    {
        public string Foreign { get; set; }
        public string Domestic { get; set; }
        public string SpotLag { get; set; }
        public string PrimaryCalendar { get; set; }
        public string SecondaryCalendar { get; set; }
    }
}
