using System.Numerics;

namespace Qwack.Math.Extensions
{
    public static class VectorExtensions
    {
        public static (double min, double max) MinMax(this double[] input)
        {
            var min = double.MaxValue;
            var max = double.MinValue;
            for (var i = 0; i < input.Length; i++)
            {
                min = System.Math.Min(input[i], min);
                max = System.Math.Max(input[i], max);
            }
            return (min, max);
        }

        public static Vector<double> Exp(this Vector<double> vector, int precision = 8)
        {
            var result = new Vector<double>(1.0);
            var term = new Vector<double>(1.0);
            
            for (var i = 1; i < precision; i++)
            {
                term = term * vector / new Vector<double>(i);
                result += term;
            }

            return result;
        }

        public static Vector<double> Exp2(this Vector<double> z)
        {
            var exp = new Vector<double>(1.0);
            var facTotal = z;
            for (var i = 1; i < 8; i++)
            {
                var fac = i.Factorial();
                exp += facTotal / new Vector<double>(fac);
                facTotal *= z;
            }
            return exp;
        }

        public static Vector<double> IntPow(this Vector<double> num, int exponent)
        {

            var result = new Vector<double>(1.0);
            var exp = System.Math.Abs(exponent);
            while (exp > 0)
            {
                if (exp % 2 == 1)
                    result *= num;
                exp >>= 1;
                num *= num;
            }

            return (exponent > 0) ? result : (new Vector<double>(1.0)) / result;
        }

        public static double[] Values(this Vector<double> val)
        {
            var o = new double[Vector<double>.Count];
            for (var i = 0; i < o.Length; i++)
                o[i] = val[i];
            return o;
        }
    }
}
