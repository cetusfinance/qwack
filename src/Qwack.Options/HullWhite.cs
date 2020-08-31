using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Math;
using Qwack.Dates;
using static System.Math;
using Qwack.Math.Extensions;
using System.Net.Http;
using static Qwack.Math.Statistics;
using Qwack.Transport.BasicTypes;
using Qwack.Core.Models;

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
        
        public double P0(double T) => DiscountCurve.GetDf(DiscountCurve.BuildDate, DateExtensions.AddYearFraction(DiscountCurve.BuildDate, T, DayCountBasis));
        public double dLogP0dt(double T) => (Log(P0(T + _tBump / 2.0)) - Log(P0(T - _tBump / 2.0))) / _tBump;
        public double B(double t, double T) => (1.0 - Exp(-Alpha * (T - t))) / Alpha;
        public double A(double t, double T) => P0(T) / P0(t) * Exp(-B(t, T) * dLogP0dt(T) - (SigmaR * SigmaR * (Exp(-Alpha * T) - Exp(-Alpha * t)).IntPow(2) * (Exp(2.0 * Alpha * t) - 1.0)) / (4.0 * Alpha * Alpha * Alpha));

        public double SigmaP(double t, double T) => 1.0 / Sqrt(t) * SigmaR / Alpha * (1.0 - Exp(-Alpha * (T - t))) * Sqrt((1.0 - Exp(-2.0 * Alpha * t)) / (2.0 * Alpha));
        public double VarP0(double t, double T) => SigmaR * SigmaR * t * (B(t, T).IntPow(2));
        public double Caplet(double K, double tFix, double tPay) => (1.0 + K * (tPay - tFix)) * ZBP(tFix, tPay, 1.0 / (1.0 + K * (tPay - tFix)));
        public double ZBP(double tFix, double tPay, double X)
        {
            var d1 = Log(P0(tFix) * X / P0(tPay)) / Sqrt(VarP0(tFix, tPay));
            var d2 = 0.5*Sqrt(VarP0(tFix, tPay));
            var dPlus = d1 + d2;
            var dMinus = d1 - d2;

            return X * P0(tFix) * NormSDist(dPlus) - P0(tPay) * NormSDist(dMinus);
        }
    }

    public class HullWhite2
    {
        private const double tBump = 1e-6;
        public static double F0(double T, IIrCurve curve) => -(curve.GetDf(0, T) - curve.GetDf(0, T + tBump)) / tBump;
        public static double F0dt(double T, IIrCurve curve) => (F0(T,curve) - F0(T + tBump,curve)) / tBump;
        public static double Theta(double T, double sigma, double alpha, IIrCurve curve) =>
            alpha * F0(T, curve) + F0dt(T, curve) + sigma * sigma / (2.0 * alpha) * (1.0 - Exp(-2 * alpha * T));
    }
}
