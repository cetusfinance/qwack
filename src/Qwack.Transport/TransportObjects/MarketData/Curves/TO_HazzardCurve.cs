using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    public class TO_HazzardCurve
    {
        public double? ConstantPD { get; set; }
        public DateTime OriginDate { get; set; }
        public DayCountBasis Basis { get; set; }
        public TO_Interpolator1d HazzardCurve { get; set; }
    }
}
