using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Options.VolSurfaces;

namespace Qwack.Options.VolSurfaces
{
    public static class LocalVol
    {
        public static double[,] ComputeLocalVarianceOnGrid(this IVolSurface VanillaSurface, double[,] strikes, double[] timeSteps, Func<double,double> forwardFunc)
        {
            var numberOfTimesteps = timeSteps.Length;
            var numberOfStrikes = strikes.GetLength(1);
            double[,] objArray;

            var deltaK = 0.0001 * forwardFunc(timeSteps[0]);
            var deltaKsqr = deltaK * deltaK;
          
            objArray = new double[numberOfTimesteps-1, numberOfStrikes];


            var Ss = new double[numberOfTimesteps];
            for (var i = 0; i < numberOfTimesteps; i++)
            {
                Ss[i] = forwardFunc(timeSteps[i]);
            }

           
            for (var id = 1; id < numberOfTimesteps ; id++)
            {
                double K, V, S, Td, dwdT, localVariance, TdSq, T, T1;
                double V_t1, V_Kp2, V_Km2, K_tm1, St;
                double y, yPlus, yPlus2, yMinus, yMinus2, dwdY, dwdY_p, dwdY_m, d2wd2Y, w, w_t1, w_kPlus2, w_kMinus2, Y1, Y2;

                T = timeSteps[id];
                T1 = timeSteps[id - 1];
                Td = T - T1;
                TdSq = System.Math.Sqrt(Td);
                S = Ss[id]; 
                St = Ss[id - 1]; 

                for (var ik = 0; ik < numberOfStrikes; ik++)
                {
                    K = strikes[id, ik];
                    K_tm1 = K * St / S;
                    y = System.Math.Log(K / S);
                    yPlus = System.Math.Log((K + deltaK) / S);
                    yPlus2 = System.Math.Log((K + deltaK * 2) / S);
                    yMinus = System.Math.Log((K - deltaK) / S);
                    yMinus2 = System.Math.Log((K - deltaK * 2) / S);
                    Y1 = System.Math.Log(System.Math.Sqrt(K * K + 2 * deltaK * K) / S);
                    Y2 = System.Math.Log(System.Math.Sqrt(K * K - 2 * deltaK * K) / S);
                    var fwd = forwardFunc(T);
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

                    objArray[id - 1, ik] = localVariance;
                }
            }


            return objArray;
        }

    }
}
