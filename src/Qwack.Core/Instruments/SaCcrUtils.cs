using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;
using static System.Math;
using static Qwack.Math.Statistics;

namespace Qwack.Core.Instruments
{
    public static class SaCcrUtils
    {
        public static double SupervisoryDelta(double fwd, double strike, double T, OptionType callPut, double supervisoryVol, double position)
        {
            var q = (Log(fwd / strike) + 0.5 * supervisoryVol * supervisoryVol * T) / (supervisoryVol * Sqrt(T));
            return callPut switch
            {
                OptionType.C => Sign(position) * NormSDist(q),
                OptionType.P => -Sign(position) * NormSDist(-q),
                _ => Sign(position),
            };
        }

        public static double SupervisoryDuration(double start, double end) => (Exp(-0.05 * start) - Exp(-0.05 * end)) / 0.05;
        public static double MfUnmargined(double m) => Sqrt(Min(Max(m, 10 / 252), 1.0) / 1.0); //floored at 10 business days
        public static double MfMargined(double marginPeriodRiskBusinessDays) => 3.0 / 2.0 * Sqrt(marginPeriodRiskBusinessDays / 252);
    }
}
