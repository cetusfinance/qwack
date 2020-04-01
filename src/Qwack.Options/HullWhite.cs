using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Math;
using Qwack.Dates;
using static System.Math;
using Qwack.Math.Extensions;

namespace Qwack.Options
{
    public class HullWhite
    {
        private const double _tBump = 0.000001;
        public double SigmaR { get; private set; }
        public double Alpha { get; private set; }
        public IInterpolator1D Theta { get; private set; }

        public IIrCurve DiscountCurve { get; private set; }
        public DayCountBasis DayCountBasis { get; set; }
        //private double A(double t, double T)
        //{

        //}

        public double P0(double T) => DiscountCurve.GetDf(DiscountCurve.BuildDate, DateExtensions.AddYearFraction(DiscountCurve.BuildDate, T, DayCountBasis));
        public double dLogP0dt(double T) => (Log(P0(T + _tBump / 2.0)) - Log(P0(T - _tBump / 2.0))) / _tBump;
        public double B(double t, double T) => (1.0 - Exp(-Alpha * (T - t))) / Alpha;
        public double A(double t, double T) => P0(T) / P0(t) * Exp(-B(t, T) * dLogP0dt(T) - (SigmaR * SigmaR * (Exp(-Alpha * T) - Exp(-Alpha * t)).IntPow(2) * (Exp(2.0 * Alpha * t) - 1.0)) / (4.0 * Alpha * Alpha * Alpha));
    }
}
