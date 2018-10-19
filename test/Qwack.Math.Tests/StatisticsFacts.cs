using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using static Qwack.Math.Statistics;

namespace Qwack.Math.Tests
{
    public class StatisticsFacts
    {
        [Fact]
        public void NormalDistributionFacts()
        {
            
            Assert.Equal(0.0, ProbabilityDensityFunction(-100),6);
            Assert.Equal(0.0, ProbabilityDensityFunction(100), 6);

            Assert.Equal(0.0, CumulativeNormalDistribution(-100), 6);
            Assert.Equal(1.0, CumulativeNormalDistribution(100), 6);

            Assert.Throws<ArgumentOutOfRangeException>(() => NormInv(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => NormInv(0.5,0.5,-1));

            Assert.Equal(double.NegativeInfinity, NormInv(0));
            Assert.Equal(double.PositiveInfinity, NormInv(1));
            Assert.Equal(0.1234, NormInv(0.2,0.1234,0.0));
            Assert.Equal(0.495301648545571, NormInv(0.9999, 0.1234, 0.1),8);

            Assert.Equal(1, Erf(6));
            Assert.Equal(0, Erf(0),8);
        }

        [Fact]
        public void VarianceFacts()
        {
            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).Variance());
            Assert.Equal(0.0, VarianceWithAverage(new[] { 0.0, 0.0, 0.0 },0.0));

            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).StdDev());
            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).StdDevWithAverage(0.0));
        }

        [Fact]
        public void SkewnessFacts()
        {
            Assert.Equal(0.0333333333333334, (new[] { 0.1, 0.2, 0.1 }).Skewness(),8);
            Assert.Equal(0.0333333333333334, StandardizedMoment(new[] { 0.1, 0.2, 0.1 },3), 8);
        }

        [Fact]
        public void AverageFacts()
        {
            Assert.Equal(0.1, (new[] { 0.1, 0.2, 0.1 }).Mode());
            Assert.Equal(0.2, (new[] { 0.01, 0.2, 0.21 }).Median());
            Assert.Equal(0.205, (new[] { 0.01, 0.2, 0.21, 0.7 }).Median(),8);
        }

        [Fact]
        public void CorrelationFacts()
        {
            Assert.Equal(1.0, (new[] { 1.0, 2.0, 3.0 }).Correlation(new[] { 1.0, 2.0, 3.0 }).Correlation);
            Assert.Equal(0.0, (new[] { 1.0, 2.0, 3.0 }).Correlation(new[] { 1.0, 2.0, 3.0 }).Error);

            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).Correlation(new[] { 1.0, 2.0, 3.0 }).Correlation);
            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).Correlation(new[] { 1.0, 2.0, 3.0 }).Error);

            Assert.Equal(0.0, (new[] { 1.0, 2.0, 3.0 }).Correlation(new[] { 0.0, 0.0, 0.0 }).Correlation);
            Assert.Equal(0.0, (new[] { 1.0, 2.0, 3.0 }).Correlation(new[] { 0.0, 0.0, 0.0 }).Error);

            Assert.Equal(1.0, (new[] { 0.0, 0.0, 0.0 }).Correlation(new[] { 0.0, 0.0, 0.0 }).Correlation);
            Assert.Equal(0.0, (new[] { 0.0, 0.0, 0.0 }).Correlation(new[] { 0.0, 0.0, 0.0 }).Error);

            Assert.Equal(double.NaN, (new[] { 1.0, 2.0, 3.0, 4.0 }).Correlation(new[] { 1.0, 2.0, 3.0 }).Correlation);
        }

        [Fact]
        public void ReturnsFacts()
        {
            Assert.True(Enumerable.SequenceEqual(new[] { 1.0, -0.5 }, (new[] { 1.0, 2.0, 1.0 }).Returns(false)));
        }
    }
}
