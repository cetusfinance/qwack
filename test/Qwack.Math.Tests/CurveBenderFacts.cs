using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Qwack.Math;
using static System.Math;

namespace Qwack.Math.Tests
{
    public class CurveBenderFacts
    {
        [Fact]
        public void BenderFacts()
        {
            var inSpreads = new double[] { 1.0, 2.0, 3.0, 2.0, 1.0 };

            var sparseSpreads = new double?[] { 1.0, null , 6.0, null, 1.0 };
            Assert.True(Enumerable.SequenceEqual(new double[] { 1.0, 3.5, 6.0, 3.5, 1.0 }, CurveBender.Bend(inSpreads, sparseSpreads)));

            sparseSpreads = new double?[] { null, null, 6.0, null, 1.0 };
            Assert.True(Enumerable.SequenceEqual(new double[] { 4, 5, 6.0, 3.5, 1.0 }, CurveBender.Bend(inSpreads, sparseSpreads)));

            sparseSpreads = new double?[] { 0, null, 6.0, null, null };
            Assert.True(Enumerable.SequenceEqual(new double[] { 0, 3, 6.0, 5.0, 4.0 }, CurveBender.Bend(inSpreads, sparseSpreads)));
        }
    }
}
