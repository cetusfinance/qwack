using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Correlation;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Options
{
    public static class LocalVol
    {
        public static double[][] ComputeLocalVarianceOnGrid(this IVolSurface VanillaSurface, double[][] strikes, double[] timeSteps, Func<double, double> forwardFunc)
        {
            var numberOfTimesteps = timeSteps.Length;
            var deltaK = 0.001 * forwardFunc(timeSteps[0]);
            var lvGrid = new double[numberOfTimesteps - 1][];

            var Ss = new double[numberOfTimesteps];
            for (var i = 0; i < Ss.Length; i++)
            {
                Ss[i] = forwardFunc(timeSteps[i]);
            }


            for (var it = 1; it < numberOfTimesteps; it++)
            {
                var numberOfStrikes = strikes[it - 1].Length;
                lvGrid[it - 1] = new double[numberOfStrikes];

                double K, V, S, Td, dwdT, localVariance, TdSq, T, T1;
                double V_t1, V_Kp2, V_Km2, K_tm1, St;
                double y, yPlus, yPlus2, yMinus, yMinus2, dwdY, dwdY_p, dwdY_m, d2wd2Y, w, w_t1, w_kPlus2, w_kMinus2; //, Y1, Y2;

                T = timeSteps[it];
                T1 = timeSteps[it - 1];
                Td = T - T1;
                TdSq = Sqrt(Td);
                S = Ss[it];
                St = Ss[it - 1];
                var fwd = forwardFunc(T);

                for (var ik = 0; ik < numberOfStrikes; ik++)
                {
                    K = strikes[it][ik];
                    K_tm1 = K * St / S;
                    y = Log(K / S);
                    yPlus = Log((K + deltaK) / S);
                    yPlus2 = Log((K + deltaK * 2) / S);
                    yMinus = Log((K - deltaK) / S);
                    yMinus2 = Log((K - deltaK * 2) / S);
                    //Y1 = Log(Sqrt(K * K + 2 * deltaK * K) / S);
                    //Y2 = Log(Sqrt(K * K - 2 * deltaK * K) / S);                  
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

                    //d2wd2Y = (dwdY_p - dwdY_m) / (Y1 - Y2);
                    d2wd2Y = (dwdY_p - dwdY_m) / (yPlus - yMinus);

                    localVariance = dwdT / (1 - y / w * dwdY + 0.25 * (-0.25 - 1 / w + (y * y / (w))) * dwdY * dwdY + 0.5 * d2wd2Y);

                    lvGrid[it - 1][ik] = localVariance;
                }
            }

            return lvGrid;
        }

        public static double[][] ComputeLocalVarianceOnGridFromCalls(this IVolSurface VanillaSurface, double[][] strikes, double[] timeSteps, Func<double, double> forwardFunc)
        {
            var numberOfTimesteps = timeSteps.Length;
            var deltaK = 0.001 * forwardFunc(timeSteps[0]);
            var lvGrid = new double[numberOfTimesteps - 1][];

            var fwds = timeSteps.Select(t => forwardFunc(t)).ToArray();

            //ParallelUtils.Instance.For(1, numberOfTimesteps, 1, it =>
            for (var it = 1; it < numberOfTimesteps; it++)
            {
                var T = timeSteps[it];
                var T1 = timeSteps[it - 1];
                var fwd = fwds[it];
                var fwdtm1 = fwds[it - 1];
                var rmq = Log(fwd / fwdtm1) / (T - T1);
                var numberOfStrikes = strikes[it - 1].Length;
                var cInterp = VanillaSurface.GeneratePremiumInterpolator(numberOfStrikes * 2, T, fwd, OptionType.C);

                lvGrid[it - 1] = new double[numberOfStrikes];

                if (numberOfStrikes > 1)
                {
                    for (var ik = 0; ik < numberOfStrikes; ik++)
                    {

                        var K = strikes[it][ik];
                        var V = VanillaSurface.GetVolForAbsoluteStrike(K, T, fwd);
                        var C = BlackFunctions.BlackPV(fwd, K, 0.0, T, V, OptionType.C);
                        var Vtm1 = VanillaSurface.GetVolForAbsoluteStrike(K, T1, fwdtm1);

                        //var dcdt = -BlackFunctions.BlackTheta(fwd, K, 0.0, T, V, OptionType.C);
                        var dcdt = -(BlackFunctions.BlackPV(fwdtm1, K, 0.0, T1, Vtm1, OptionType.C) - C) / (T - T1);
                        var dcdk = cInterp.FirstDerivative(K);
                        var d2cdk2 = cInterp.SecondDerivative(K);

                        var localVariance = d2cdk2 == 0 ? V * V : (dcdt - rmq * (C - K * dcdk)) / (0.5 * K * K * d2cdk2);
                        lvGrid[it - 1][ik] = localVariance;
                    }
                }
                else
                {
                    var K = strikes[it][0];
                    var V = VanillaSurface.GetVolForAbsoluteStrike(K, T, fwd);
                    lvGrid[it - 1][0] = V * V;
                }
            }//, false).Wait();

            return lvGrid;
        }

        public static List<double[][]> LocalCorrelationRaw(this IAssetFxModel model, double[] times)
        {
            var o = new List<double[][]>();
            var matrix = model.CorrelationMatrix;

            for (var it = 0; it < times.Length; it++)
            {
                o.Add(new double[matrix.LabelsX.Length][]);
                for (var ix = 0; ix < matrix.LabelsX.Length; ix++)
                {
                    o[it][ix] = new double[matrix.LabelsY.Length];
                }
            }

            for (var ix = 0; ix < matrix.LabelsX.Length; ix++)
            {
                for (var iy = 0; iy < matrix.LabelsY.Length; iy++)
                {
                    var labelX = matrix.LabelsX[ix];
                    var labelY = matrix.LabelsY[iy];
                    var lastVarBasket = 0.0;
                    var tLast = 0.0;
                    for (var it = 0; it < times.Length; it++)
                    {
                        var t = times[it];
                        var dt = t - tLast;
                        var termCorrel = matrix.GetCorrelation(labelX, labelY, t);
                        var volX = ((IATMVolSurface)model.GetVolSurface(labelX)).GetForwardATMVol(0, t);
                        var volY = ((IATMVolSurface)model.GetVolSurface(labelY)).GetForwardATMVol(0, t);
                        var termVar = (volX * volX + volY * volY + 2 * termCorrel * volX * volY) * t;
                        var incrementalVar = (termVar - lastVarBasket) / dt;
                        var volXfwd = ((IATMVolSurface)model.GetVolSurface(labelX)).GetForwardATMVol(tLast, t);
                        var volYfwd = ((IATMVolSurface)model.GetVolSurface(labelY)).GetForwardATMVol(tLast, t);
                        var localCorrel = (incrementalVar - volXfwd * volXfwd - volYfwd * volYfwd) / (2 * volXfwd * volYfwd);
                        o[it][ix][iy] = (t == 0) ? termCorrel : localCorrel;
                        tLast = t;
                        lastVarBasket = termVar;
                    }
                }
            }

            return o;
        }

        public static List<CorrelationMatrix> LocalCorrelationObjects(this IAssetFxModel model, double[] times)
        {
            var o = new List<double[][]>();
            var matrix = model.CorrelationMatrix;

            for (var it = 0; it < times.Length; it++)
            {
                o.Add(new double[matrix.LabelsX.Length][]);
            }

            for (var ix = 0; ix < matrix.LabelsX.Length; ix++)
            {
                for (var iy = 0; iy < matrix.LabelsY.Length; iy++)
                {
                    var labelX = matrix.LabelsX[ix];
                    var labelY = matrix.LabelsY[iy];
                    var lastVarBasket = 0.0;
                    var tLast = 0.0;
                    for (var it = 0; it < times.Length; it++)
                    {
                        if (o[it] == null)
                            o[it] = new double[matrix.LabelsX.Length][];
                        if (o[it][ix] == null)
                            o[it][ix] = new double[matrix.LabelsY.Length];
                        var t = times[it];
                        var dt = t - tLast;
                        var termCorrel = matrix.GetCorrelation(labelX, labelY, t);
                        var volX = ((IATMVolSurface)model.GetVolSurface(labelX)).GetForwardATMVol(0, t);
                        var volY = ((IATMVolSurface)model.GetVolSurface(labelY)).GetForwardATMVol(0, t);
                        var termVar = (volX * volX + volY * volY + 2 * termCorrel * volX * volY) * t;
                        var incrementalVar = (termVar - lastVarBasket) / dt;
                        var volXfwd = ((IATMVolSurface)model.GetVolSurface(labelX)).GetForwardATMVol(tLast, t);
                        var volYfwd = ((IATMVolSurface)model.GetVolSurface(labelY)).GetForwardATMVol(tLast, t);
                        var localCorrel = (incrementalVar - volXfwd * volXfwd - volYfwd * volYfwd) / (2 * volXfwd * volYfwd);
                        o[it][ix][iy] = (t == 0) ? termCorrel : localCorrel;
                        tLast = t;
                        lastVarBasket = termVar;
                    }
                }
            }

            return o.Select(m => new CorrelationMatrix(matrix.LabelsX, matrix.LabelsY, m)).ToList();
        }
    }
}
