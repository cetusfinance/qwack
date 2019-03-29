using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Qwack.Math;
using static System.Math;
using static Qwack.Math.BesselFunctions;

namespace Qwack.Math.Tests
{
    public class BesselFunctionFacts
    {
        [Fact]
        public void BesselFacts()
        {
            Assert.Equal(1.0, Jn(0, 0));
            Assert.Equal(0.0, Jn(0, 1));

            Assert.Equal(0.778800783071148, Jn(1, 0), 8);
            Assert.Equal(0.442398433857704, Jn(1, 1), 8);
        }
    }
}
