using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    public class TO_RiskyFlySurface : TO_GridVolSurface
    {
        public double[][] Riskies { get; set; }
        public double[][] Flies { get; set; }
        public double[] ATMs { get; set; }
        public double[] WingDeltas { get; set; }
        public double[] Forwards { get; set; }
        public WingQuoteType WingQuoteType { get; set; }
        public AtmVolType AtmVolType { get; set; }

    }
}
