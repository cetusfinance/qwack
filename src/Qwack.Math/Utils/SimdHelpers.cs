using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math.Utils
{
    internal class SimdHelpers
    {
        internal static void PadArrayForSIMDnoAlloc(double[] X0, double[] Y0, double[] X, double[] Y, int overFlow, double padValueX, double padValueY)
        {
            switch (overFlow)
            {
                case 1:
                    X[X0.Length] = padValueX;
                    Y[X0.Length] = padValueY;
                    break;
                case 2:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    break;
                case 3:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    break;
                case 4:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    break;
                case 5:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    break;
                case 6:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    X[X0.Length + 5] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    Y[X0.Length + 5] = padValueY;
                    break;
                case 7:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    X[X0.Length + 5] = padValueX;
                    X[X0.Length + 6] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    Y[X0.Length + 5] = padValueY;
                    Y[X0.Length + 6] = padValueY;
                    break;
            }
        }

        internal static void PadArrayForSIMD(double[] X0, double[] Y0, out double[] X, out double[] Y, int overFlow, double padValueX, double padValueY)
        {
            if (X0.Length != Y0.Length)
                throw new NotImplementedException();

            if (overFlow == 0)
            {
                X = X0;
                Y = Y0;
            }
            else
            {
                X = new double[X0.Length + overFlow];
                Y = new double[Y0.Length + overFlow];
                X0.CopyTo(X, 0);
                Y0.CopyTo(Y, 0);
            }

            switch (overFlow)
            {
                case 1:
                    X[X0.Length] = padValueX;
                    Y[X0.Length] = padValueY;
                    break;
                case 2:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    break;
                case 3:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    break;
                case 4:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    break;
                case 5:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    break;
                case 6:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    X[X0.Length + 5] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    Y[X0.Length + 5] = padValueY;
                    break;
                case 7:
                    X[X0.Length] = padValueX;
                    X[X0.Length + 1] = padValueX;
                    X[X0.Length + 2] = padValueX;
                    X[X0.Length + 3] = padValueX;
                    X[X0.Length + 4] = padValueX;
                    X[X0.Length + 5] = padValueX;
                    X[X0.Length + 6] = padValueX;
                    Y[X0.Length] = padValueY;
                    Y[X0.Length + 1] = padValueY;
                    Y[X0.Length + 2] = padValueY;
                    Y[X0.Length + 3] = padValueY;
                    Y[X0.Length + 4] = padValueY;
                    Y[X0.Length + 5] = padValueY;
                    Y[X0.Length + 6] = padValueY;
                    break;
            }
        }
    }
}
