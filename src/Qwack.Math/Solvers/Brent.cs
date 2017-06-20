using System;

namespace Qwack.Math.Solvers
{
    /// <summary>
    /// Implementation of Brent's method for root finding
    /// </summary>

    // C# reference code taken from https://en.wikipedia.org/w/index.php?title=Brent%27s_method&oldid=605428601

    public static class Brent
    {
        public static double BrentsMethodSolve(Func<double, double> function, double lowerLimit, double upperLimit, double errorTol)
        {
            var a = lowerLimit;
            var b = upperLimit;
            var c = 0.0;
            var d = double.MaxValue;

            var fa = function(a);
            var fb = function(b);

            var fc = 0.0;
            var s = 0.0;
            var fs = 0.0;

            // if f(a) f(b) >= 0 then error-exit
            if (fa * fb >= 0)
            {
                if (fa < fb)
                    return a;
                else
                    return b;
            }

            // if |f(a)| < |f(b)| then swap (a,b) end if
            if (System.Math.Abs(fa) < System.Math.Abs(fb))
            { var tmp = a; a = b; b = tmp; tmp = fa; fa = fb; fb = tmp; }

            c = a;
            fc = fa;
            var mflag = true;
            var i = 0;

            while (fb != 0 && (System.Math.Abs(a - b) > errorTol))
            {
                if ((fa != fc) && (fb != fc))
                    // Inverse quadratic interpolation
                    s = a * fb * fc / (fa - fb) / (fa - fc) + b * fa * fc / (fb - fa) / (fb - fc) + c * fa * fb / (fc - fa) / (fc - fb);
                else
                    // Secant Rule
                    s = b - fb * (b - a) / (fb - fa);

                var tmp2 = (3 * a + b) / 4;
                if ((!(((s > tmp2) && (s < b)) || ((s < tmp2) && (s > b)))) || (mflag && (System.Math.Abs(s - b) >= (System.Math.Abs(b - c) / 2))) || (!mflag && (System.Math.Abs(s - b) >= (System.Math.Abs(c - d) / 2))))
                {
                    s = (a + b) / 2;
                    mflag = true;
                }
                else
                {
                    if ((mflag && (System.Math.Abs(b - c) < errorTol)) || (!mflag && (System.Math.Abs(c - d) < errorTol)))
                    {
                        s = (a + b) / 2;
                        mflag = true;
                    }
                    else
                        mflag = false;
                }
                fs = function(s);
                d = c;
                c = b;
                fc = fb;
                if (fa * fs < 0) { b = s; fb = fs; }
                else { a = s; fa = fs; }

                // if |f(a)| < |f(b)| then swap (a,b) end if
                if (System.Math.Abs(fa) < System.Math.Abs(fb))
                { var tmp = a; a = b; b = tmp; tmp = fa; fa = fb; fb = tmp; }
                i++;
                if (i > 1000)
                    throw new Exception($"Error is {fb}");
            }
            return b;
        }
    }
}
