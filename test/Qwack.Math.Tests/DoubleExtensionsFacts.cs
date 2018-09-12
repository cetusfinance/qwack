using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Qwack.Math.Extensions;
using static System.Math;

namespace Qwack.Math.Tests
{
    public class DoubleExtensionsFacts
    {
        [Fact]
        public void SubArrayFacts()
        {
            var arr = new[] { 1, 2, 3, 4, 5, 6 };
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 2, 3 }, arr.SubArray(0, 3)));
            Assert.True(Enumerable.SequenceEqual(new[] { 2, 3, 4, 5 }, arr.SubArray(1, 4)));
        }

        [Fact]
        public void SafeMaxMinSignFacts()
        {
            Assert.Equal(1.0, 1.0.SafeMax(double.NaN));
            Assert.Equal(1.0, 1.0.SafeMin(double.NaN));
            Assert.Equal(1.0, double.NaN.SafeMax(1.0));
            Assert.Equal(1.0, double.NaN.SafeMin(1.0));
            Assert.Equal(1.0, 0.5.SafeMax(1.0));
            Assert.Equal(1.0, 2.0.SafeMin(1.0));

            Assert.Equal(0, double.NaN.SafeSign());
            Assert.Equal(-1.0, -7.0.SafeSign());
        }

        [Fact]
        public void MiscExtensionFacts()
        {
            Assert.Equal(1.0 / Sqrt(2.0 * PI), 0.0.Phi(), 10);

            Assert.Equal(2, 1.7.Round(0));
            Assert.Equal(1.7, 1.72.Round(1));

            Assert.Equal(2, 4.0.Sqrt());

            Assert.Equal(2, 2.0.Abs());
            Assert.Equal(2, (-2.0).Abs());

            Assert.Equal(4, 2.0.Pow(2));
            Assert.Equal(4, 2.0.IntPow(2));
            Assert.Equal(-8, (-2.0).IntPow(3));
            Assert.Equal(4, 2.IntPow(2));
            Assert.Equal(-8, (-2).IntPow(3));

            Assert.Equal(6, 3.Factorial());
        }

        [Fact]
        public void DoubleArrayFacts()
        {
            var e = new double[3, 3] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };

            Assert.True(Enumerable.SequenceEqual(new double[] { 1, 2, 3 }, e.GetRow(0)));
            Assert.True(Enumerable.SequenceEqual(new double[] { 7, 8, 9 }, e.GetRow(2)));
        }
    }
}
