using System;

namespace Qwack.Core.Basic
{
    public class Commodity2FactorCalibrationSettings
    {
        // Initial guesses (optional)
        public double? InitialSigma1 { get; set; }
        public double? InitialSigma2 { get; set; }
        public double? InitialKappa { get; set; }
        public double? InitialRho { get; set; }

        // Calibration control
        public double Tolerance { get; set; } = 1e-8;
        public int MaxIterations { get; set; } = 10000;
        public DateTime[] CalibrationMaturities { get; set; }

        // Swaption calibration
        public bool CalibrateToSwaptions { get; set; } = false;
        public SwaptionVolData SwaptionVols { get; set; }
    }
}
