using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Random.Sobol;
using Xunit;

namespace Qwack.Math.Tests.Random
{
    public class SobolFacts
    {
        private static readonly string s_directionNumbers = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "SobolDirectionNumbers.txt");

        [Fact]
        public void SmallDimsWorks()
        {
            var dimensions = 3;
            var paths = 9;

            var dn = new SobolDirectionNumbers();
            dn.LoadFromFile(s_directionNumbers);
            var generator = new SobolGenerator(dn)
            {
                Dimensions = dimensions
            };
            var results = new double[paths * dimensions];
            generator.GetPathsRaw(paths, ref results);

            var expectSequence = new double[]
            {
                0.5, 0.5, 0.5,
                0.75, 0.25, 0.25,
                0.25, 0.75, 0.75,
                0.375, 0.375, 0.625,
                0.875, 0.875, 0.125,
                0.625, 0.125, 0.875,
                0.125, 0.625, 0.375,
                0.1875, 0.3125, 0.9375,
                0.6875, 0.8125, 0.4375,
            };
            Assert.Equal(expectSequence, results);
        }
    }
}
