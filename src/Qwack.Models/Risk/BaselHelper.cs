using Qwack.Math.Extensions;
using static System.Math;
using static Qwack.Math.Statistics;

namespace Qwack.Models.Risk
{
    public static class BaselHelper
    {
        public static double R(double PD) => 0.12 * (1 - Exp(-50 * PD)) / (1 - Exp(-50)) + 0.24 * (1.0 - (1 - Exp(-50 * PD)) / (1 - Exp(-50)));
        public static double B(double PD) => (0.11852 - 0.05478 * Log(PD)).IntPow(2);
        public static double K(double PD, double LGD, double M) =>
            //LGD * (NormSDist(NormInv(PD)/Sqrt(1-R(PD))+Sqrt(R(PD)/(1-R(PD)))*NormInv(0.999))-PD) * (1 + (M - 2.5) * b(PD)) / (1 - 1.5 * b(PD));
            (LGD * NormSDist(Pow(1 - R(PD), -0.5) * NormInv(PD) + Sqrt(R(PD) / (1 - R(PD))) * NormInv(0.999)) - PD * LGD) * Pow(1 - 1.5 * B(PD), -1) * (1 + (M - 2.5) * B(PD));
        public static double RWA(double PD, double LGD, double M, double EAD) =>
            EAD * 12.5 * K(PD, LGD, M);

        public static class BasicCvaB3
        {
            public const double Beta = 0.5;
            public const double Rho = 0.5;
            public static double kSpreadUnhedgedSingle(double riskWeight, double effMaturity, double ead) => Sc(riskWeight, effMaturity, ead);
            public static double Sc(double riskWeight, double effMaturity, double ead) => riskWeight / 1.4 * effMaturity * ead;
            public static double kEE(double riskWeight, double effMaturity, double ead) => Beta * kSpreadUnhedgedSingle(riskWeight, effMaturity, ead);
            public static double CvaCapitalCharge(double riskWeight, double effMaturity, double ead) => (1 + Beta) * kSpreadUnhedgedSingle(riskWeight, effMaturity, ead);
        }

        public static class StandardCvaB3
        {
            public static double CvaCapitalCharge(double riskWeight, double effMaturity, double ead) => 2.33 * riskWeight * effMaturity * ead * Df(effMaturity);
            public static double Df(double effMaturity) => effMaturity == 0 ? 1.0 : (1.0 - Exp(-0.05 * effMaturity)) / (0.05 * effMaturity);
        }
    }
}
