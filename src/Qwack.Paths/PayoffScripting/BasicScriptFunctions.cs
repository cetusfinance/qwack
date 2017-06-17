using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Qwack.Paths.PayoffScripting
{
    public static class BasicScriptFunctions
    {
        public static double Min(double x, double y) => System.Math.Min(x, y);

        [ScriptingFunction(IsVector = true)]
        public static Vector<double> Min(Vector<double> x, Vector<double> y) => Vector.Max(x, y);
    }
}
