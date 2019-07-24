using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Options.VolSurfaces
{
    public static class VolUtils
    {
        public static double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward, Func<double,double> GetVolForAbsoluteStrike)
        {
            var fwd = forward;
            var cp = OptionType.Put;

            Func<double, double> testFunc = (absK =>
            {
                var vol = GetVolForAbsoluteStrike(absK);
                var deltaK = System.Math.Abs(BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol, cp));
                return deltaK - System.Math.Abs(deltaStrike);
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, fwd / 10, 50 * fwd, 1e-8);

            return GetVolForAbsoluteStrike(solvedStrike);
        }

        public static double GetForwardATMVol(double start, double end, double fwdStart, double fwdEnd, Func<double,double,double,double> GetVolForAbsoluteStrike)
        {
            if (start > end)
                throw new Exception("Start must be strictly less than end");

            if (start == end)
                return start == 0 ? 0.0 : GetVolForAbsoluteStrike(fwdStart, start, fwdStart);

            var vStart = start == 0 ? 0.0 : GetVolForAbsoluteStrike(fwdStart, start, fwdStart);
            vStart *= vStart * start;

            var vEnd = GetVolForAbsoluteStrike(fwdEnd, end, fwdEnd);
            vEnd *= vEnd * end;

            var vDiff = vEnd - vStart;
            if (vDiff < 0)
                throw new Exception("Negative forward variance detected");

            return System.Math.Sqrt(vDiff / (end - start));
        }
    }
}
