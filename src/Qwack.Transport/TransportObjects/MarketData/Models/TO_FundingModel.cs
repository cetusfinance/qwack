using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    public class TO_FundingModel
    {
        public Dictionary<string, TO_IrCurve> Curves { get;  set; }
        public Dictionary<string, TO_VolSurface> VolSurfaces { get; set; }
        public DateTime BuildDate { get; set; }
        public TO_FxMatrix FxMatrix { get; set; }

    }
}
