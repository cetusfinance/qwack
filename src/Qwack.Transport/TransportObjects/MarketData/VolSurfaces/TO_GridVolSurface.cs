using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Interpolators;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    public class TO_GridVolSurface : TO_VolSurface_Base
    {
        public string OverrideSpotLag { get; set; }
        public double[] Strikes { get; set; }
        public StrikeType StrikeType { get; set; }
        public Interpolator1DType StrikeInterpolatorType { get; set; } 
        public Interpolator1DType TimeInterpolatorType { get; set; } 
        public double[][] Volatilities { get; set; }
        public DateTime[] Expiries { get; set; }
        public string[] PillarLabels { get; set; }
        public DayCountBasis TimeBasis { get; set; } 
        public bool FlatDeltaSmileInExtreme { get; set; }
        public double FlatDeltaPoint { get; set; } 
        public TO_Interpolator1d[] Interpolators { get; set; }
    }
}
