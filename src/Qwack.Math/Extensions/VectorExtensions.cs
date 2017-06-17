using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
