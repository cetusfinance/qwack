using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Extensions;
using static Qwack.Math.Statistics; 
using static System.Math;

namespace Qwack.Models.Risk
{
    public static class BaselHelper
    {
        public static double R(double PD) => 0.12 * (1-Exp(-50*PD))/(1-Exp(-50)) + 0.24 * (1.0 - (1 - Exp(-50 * PD)) / (1 - Exp(-50)));
        public static double b(double PD) => (0.11852 - 0.05478 * Log(PD)).IntPow(2);
        public static double K(double PD, double LGD, double M) =>
            LGD * (NormSDist(NormInv(PD)/Sqrt(1-R(PD))+Sqrt(R(PD)/(1-R(PD)))*NormInv(0.999))-PD) * (1 + (M - 2.5) * b(PD)) / (1 - 1.5 * b(PD));
        public static double RWA(double PD, double LGD, double M, double EAD) =>
            EAD * 12.5 * K(PD, LGD, M);
    }
}
