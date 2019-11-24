using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Math;
using Qwack.Providers.Json;
using Qwack.Math;
using Qwack.Math.Distributions;

namespace Qwack.Excel.Tests.Math
{
    public class MathFunctionsFacts
    {
        [Fact]
        public void FisherTransform_Facts() => Assert.Equal(Statistics.FisherTransform(0.5, 0.75, 100, true), MathFunctions.FisherTransform(0.5, 0.75, 100, true));

        [Fact]
        public void BivariateNormalStdPDF_Facts() => Assert.Equal(BivariateNormal.PDF(.5, .5, .5), MathFunctions.BivariateNormalStdPDF(.5, .5, .5));

        [Fact]
        public void BivariateNormalPDF_Facts() => Assert.Equal(BivariateNormal.PDF(.5, .5, .5, .5, .5, .5, .5), MathFunctions.BivariateNormalPDF(.5, .5, .5, .5, .5, .5, .5));
        
    }
}
