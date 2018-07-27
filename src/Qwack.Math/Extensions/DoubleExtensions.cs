using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Extensions
{

    public static class DoubleExtensions
    {
        public const double Sqrt2Pi = 2.506628274631000502415765284811;

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static double SafeMax(this double input1, double input2)
        {

            if (double.IsNaN(input1))
                return input2;
            else if (double.IsNaN(input2))
                return input1;
            else
                return System.Math.Max(input1, input2);

        }
        public static double SafeMin(this double input1, double input2)
        {

            if (double.IsNaN(input1))
                return input2;
            else if (double.IsNaN(input2))
                return input1;
            else
                return System.Math.Min(input1, input2);

        }
        public static double SafeSign(this double input)
        {
            if (double.IsNaN(input))
                return 0;
            else
                return System.Math.Sign(input);
        }

        public static double Phi(this double x) => (System.Math.Exp((-x * x) / 2.0) / Sqrt2Pi);

        public static double Round(this double x, int nDigits) => System.Math.Round(x, nDigits);
        public static double Abs(this double x) => System.Math.Abs(x);
        public static double Sqrt(this double x)=> System.Math.Sqrt(x);
        public static double Pow(this double x, double power) => System.Math.Pow(x, power);

        public static double IntPow(this double num, int exponent)
        {
            var result = 1.0;
            var exp = System.Math.Abs(exponent);
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= num;
                exp >>= 1;
                num *= num;
            }

            return (exponent > 0) ? result : 1.0 / result;
        }
        public static int IntPow(this int num, int exponent)
        {
            var result = 1.0;
            var exp = System.Math.Abs(exponent);
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= num;
                exp >>= 1;
                num *= num;
            }

            return (int)((exponent > 0) ? result : 1.0 / result);
        }

        public static int Factorial(this int x)
        {
            if (x == 1)
                return 1;
            else
                return x * Factorial(x - 1);
        }

        #region DoubleArray Extensions
        public static double[] GetRow(this double[,] z, int row)
        {
            var r = new double[z.GetLength(1)];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = z[row, i];
            }
            return r;
        }
        #endregion
    }
}


