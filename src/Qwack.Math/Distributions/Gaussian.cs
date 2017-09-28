using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math.Distributions
{
    public static class Gaussian
    {
        public static double GKern(double X, double Xmean, double Bandwidth) => System.Math.Exp(-0.5 * System.Math.Pow((X - Xmean) / Bandwidth, 2));
    }
}
