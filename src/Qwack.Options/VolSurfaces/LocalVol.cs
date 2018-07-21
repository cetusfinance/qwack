using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Options.VolSurfaces;
using Qwack.Core.Basic;

namespace Qwack.Options
{
    public static class LocalVol
    {
        public static double[][] ComputeLocalVarianceOnGrid(this IVolSurface VanillaSurface, double[][] strikes, double[] timeSteps, Func<double,double> forwardFunc)
        {
            var numberOfTimesteps = timeSteps.Length;
            var numberOfStrikes = strikes[0].Length;
        
            var deltaK = 0.0001 * forwardFunc(timeSteps[0]);
            var deltaKsqr = deltaK * deltaK;
          
            var lvGrid = new double[numberOfTimesteps-1][];


            var Ss = new double[numberOfTimesteps];
            for (var i = 0; i < numberOfTimesteps; i++)
            {
                Ss[i] = forwardFunc(timeSteps[i]);
            }

           
            for (var it = 1; it < numberOfTimesteps ; it++)
            {
                lvGrid[it - 1] = new double[numberOfStrikes];

                double K, V, S, Td, dwdT, localVariance, TdSq, T, T1;
                double V_t1, V_Kp2, V_Km2, K_tm1, St;
                double y, yPlus, yPlus2, yMinus, yMinus2, dwdY, dwdY_p, dwdY_m, d2wd2Y, w, w_t1, w_kPlus2, w_kMinus2, Y1, Y2;

                T = timeSteps[it];
                T1 = timeSteps[it - 1];
                Td = T - T1;
                TdSq = System.Math.Sqrt(Td);
                S = Ss[it]; 
                St = Ss[it - 1];
                var fwd = forwardFunc(T);

                for (var ik = 0; ik < numberOfStrikes; ik++)
                {
                    K = strikes[it][ik];
                    K_tm1 = K * St / S;
                    y = System.Math.Log(K / S);
                    yPlus = System.Math.Log((K + deltaK) / S);
                    yPlus2 = System.Math.Log((K + deltaK * 2) / S);
                    yMinus = System.Math.Log((K - deltaK) / S);
                    yMinus2 = System.Math.Log((K - deltaK * 2) / S);
                    Y1 = System.Math.Log(System.Math.Sqrt(K * K + 2 * deltaK * K) / S);
                    Y2 = System.Math.Log(System.Math.Sqrt(K * K - 2 * deltaK * K) / S);                  
                    V = VanillaSurface.GetVolForAbsoluteStrike(K, T, fwd);
                    w = V * V * T;
                    V_t1 = VanillaSurface.GetVolForAbsoluteStrike(K_tm1, T1, fwd);
                    w_t1 = V_t1 * V_t1 * T1;
                    V_Kp2 = VanillaSurface.GetVolForAbsoluteStrike(K + deltaK * 2, T, fwd);
                    w_kPlus2 = V_Kp2 * V_Kp2 * T;
                    V_Km2 = VanillaSurface.GetVolForAbsoluteStrike(K - deltaK * 2, T, fwd);
                    w_kMinus2 = V_Km2 * V_Km2 * T;


                    dwdT = (w - w_t1) / Td;
                    dwdY_m = (w - w_kMinus2) / (y - yMinus2);
                    dwdY_p = (w_kPlus2 - w) / (yPlus2 - y);
                    dwdY = (dwdY_m + dwdY_p) / 2;

                    d2wd2Y = (dwdY_p - dwdY_m) / (Y1 - Y2);

                    localVariance = dwdT / (1 - y / w * dwdY + 0.25 * (-0.25 - 1 / w + (y * y / (w * w))) * dwdY * dwdY + 0.5 * d2wd2Y);

                    lvGrid[it - 1][ik] = localVariance;
                }
            }

            return lvGrid;
        }

    }
}
