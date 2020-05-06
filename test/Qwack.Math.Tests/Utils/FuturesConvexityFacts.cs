using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math.Utils;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Math.Tests.Utils
{
    public class FuturesConvexityFacts
    {
        [Fact]
        public void ConvexityAdjustmentAsExample()
        {
            var valDate = new DateTime(2017, 01, 31);
            var expiry = new DateTime(2020, 01, 31);
            var depoExpiry = new DateTime(2020, 04, 30);
            var volatility = 0.012;

            var adjustment = FuturesConvexityUtils.CalculateConvexityAdjustment(valDate, expiry, depoExpiry, volatility, DayCountBasis.ThirtyE360);

            var exampleExpected = 0.0007;
            Assert.Equal(exampleExpected, adjustment, 4);
        }
    }
}
