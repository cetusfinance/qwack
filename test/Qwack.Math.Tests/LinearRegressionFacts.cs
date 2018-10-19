using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Math.Tests
{
    public class LinearRegressionFacts
    {
        [Fact(Skip = "Failure")]
        public void VectorAndNoneVectorLinearRegressionMatches()
        {
            var rand = new System.Random();

            var xArray = new double[50];
            var yArray = new double[50];

            for(var i = 0; i < xArray.Length;i++)
            {
                xArray[i] = rand.NextDouble();
                yArray[i] = rand.NextDouble();
            }

            var reg1 = LinearRegression.LinearRegressionNoVector(xArray, yArray, false);
            var reg2 = LinearRegression.LinearRegressionVector(xArray, yArray);

            Assert.Equal(reg1.Alpha, reg2.Alpha);
            Assert.Equal(reg1.Beta, reg2.Beta);
            Assert.Equal(reg1.R2, reg2.R2);
        }
    }
}
