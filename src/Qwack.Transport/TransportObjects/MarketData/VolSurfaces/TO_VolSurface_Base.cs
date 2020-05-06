using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    public class TO_VolSurface_Base
    {
        public DateTime OriginDate { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public string AssetId { get; set; }
    }
}
