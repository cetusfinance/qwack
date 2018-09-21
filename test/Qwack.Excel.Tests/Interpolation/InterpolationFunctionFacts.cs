using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Interpolation;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Interpolation
{
    public class InterpolationFunctionFacts
    {
        [Fact]
        public void Create1dInterpolator_Facts()
        {
            Assert.Equal("Could not parse 1d interpolator type - blah", InterpolatorFunctions.Create1dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, "blah"));
            Assert.Equal("pwah¬0", InterpolatorFunctions.Create1dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, "Linear"));
        }

        [Fact]
        public void Create2dInterpolator_Facts()
        {
            Assert.Equal("Could not parse 2d interpolator type - blah", InterpolatorFunctions.Create2dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 },new[,] { { 1.0 } }, "blah"));
            Assert.Equal("pwah¬0", InterpolatorFunctions.Create2dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, new[,] { { 1.0 } }, "Bilinear"));
        }

        [Fact]
        public void Interpolate1d_Facts()
        {
            Assert.Equal("1d interpolator blah not found in cache", InterpolatorFunctions.Interpolate1d("blah", 0));
            InterpolatorFunctions.Create1dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, "Linear");
            Assert.Equal(1.0, InterpolatorFunctions.Interpolate1d("pwah", 0));
        }

        [Fact]
        public void Interpolate2d_Facts()
        {
            Assert.Equal("2d interpolator blah not found in cache", InterpolatorFunctions.Interpolate2d("blah", 0 ,0));
            InterpolatorFunctions.Create2dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, new[,] { { 1.0 } }, "Bilinear");
            Assert.Equal(1.0, InterpolatorFunctions.Interpolate2d("pwah", 0, 0));
        }

        [Fact]
        public void Interpolate1dAverage_Facts()
        {
            Assert.Equal("1d interpolator blah not found in cache", InterpolatorFunctions.Interpolate1dAverage("blah", new[] { 0.0 }));
            InterpolatorFunctions.Create1dInterpolator("pwah", new[] { 1.0 }, new[] { 1.0 }, "Linear");
            Assert.Equal(1.0, InterpolatorFunctions.Interpolate1dAverage("pwah", new[] { 0.0 }));
        }
    }
}
