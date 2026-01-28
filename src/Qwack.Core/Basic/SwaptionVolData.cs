using System;

namespace Qwack.Core.Basic
{
    public class SwaptionVolData
    {
        public DateTime[] OptionExpiries { get; set; }    // Swaption expiry dates
        public DateTime[] SwapStartDates { get; set; }    // Underlying swap start
        public DateTime[] SwapEndDates { get; set; }      // Underlying swap end
        public double[] ImpliedVols { get; set; }         // Market swaption vols
        public double[] Weights { get; set; }             // Calibration weights (optional)
    }
}
