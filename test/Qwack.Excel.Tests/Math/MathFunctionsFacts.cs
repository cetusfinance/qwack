using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Math;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;
using Qwack.Math;

namespace Qwack.Excel.Tests.Math
{
    public class MathFunctionsFacts
    {
        [Fact]
        public void FisherTransform_Facts()
        {
            Assert.Equal(Statistics.FisherTransform(0.5, 0.75, 100, true), MathFunctions.FisherTransform(0.5, 0.75, 100, true));
        }

    

    }
}
