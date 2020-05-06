using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    public class TO_IrCurve
    {
        public DateTime[] Pillars { get; set; }
        public double[] Rates { get; set; }
        public DateTime BuildDate { get; set; }
        public string Name { get; set; }
        public Interpolator1DType InterpKind { get; set; }
        public string Ccy { get; set; }
        public string CollateralSpec { get; set; }
        public RateType RateStorageType { get; set; } = RateType.CC;
        public DayCountBasis Basis { get; set; } = DayCountBasis.ACT365F;
    }
}
