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
    }
}
