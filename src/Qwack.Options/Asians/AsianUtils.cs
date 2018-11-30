using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Options.Asians
{
    public static class AsianUtils
    {
        public static double AdjustedStrike(double K, double knownAverage, double tExpiry, double tAvgStart)
        {
            if (tAvgStart >= 0) return K;

            var t2 = tExpiry - tAvgStart;
            K = K * t2 / tExpiry - knownAverage * (t2 - tExpiry) / tExpiry;
            return K;
        }
    }
}
