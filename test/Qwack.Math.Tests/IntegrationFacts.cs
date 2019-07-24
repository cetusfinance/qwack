using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Qwack.Math.Integration;
using static System.Math;
using Xunit;

namespace Qwack.Math.Tests
{
    public class IntegrationFacts
    {
      
        [Fact]
        public void TrapizoidRuleFacts()
        {
            var f = new Func<double, double>(t => t * 2.0);
            Assert.Equal(0, TrapezoidRule(f, 0, 0, 10));
            Assert.Equal(100, TrapezoidRule(f, 0, 10, 10));

            f = new Func<double, double>(t => t * t);
            var xs = Enumerable.Range(0, 11);
            var fx = xs.Select(x => f(x)).ToArray();
            var expected = fx.Select((x, ix) => ix == 0 ? 0 : (x + fx[ix - 1]) / 2.0).Sum();
            Assert.Equal(expected, TrapezoidRule(f, 0, 10, 10));
        }

        [Fact]
        public void Trapizoid2dRuleFacts()
        {
            //examples from https://en.wikipedia.org/wiki/Multiple_integral
            var fxy = new Func<double, double, double>((x, y) => 2);
            Assert.Equal(12, TwoDimensionalTrapezoid(fxy, 3, 6, 2, 4, 10), 10);

            fxy = new Func<double, double, double>((x, y) => x * x + 4 * y);
            Assert.Equal(1719.0054, TwoDimensionalTrapezoid(fxy, 11, 14, 7, 10, 50), 10);

            fxy = new Func<double, double, double>((x, y) => 9 * Exp(-x * x * x - y * y * y));
            Assert.Equal(5.86869, TwoDimensionalTrapezoid(fxy, 0, 1, 0, 1, 20), 2);
        }

        [Fact]
        public void SimpsonsRuleFacts()
        {
            var f = new Func<double, double>(t => t * 2.0);
            Assert.Throws<Exception>(() => SimpsonsRule(f, 0, 7, 11));
            Assert.Equal(0, SimpsonsRule(f, 0, 0, 10));
            Assert.Equal(100, SimpsonsRule(f, 0, 10, 10));

            f = new Func<double, double>(t => t * t);
            Assert.Equal(333.3333333, SimpsonsRule(f, 0, 10, 10), 5);

        }

        [Fact]
        public void SimpsonsRule2dFacts()
        {

            //examples from https://en.wikipedia.org/wiki/Multiple_integral
            var fxy = new Func<double, double, double>((x, y) => 2);
            Assert.Equal(12, TwoDimensionalSimpsons(fxy, 3, 6, 2, 4, 10), 10);

            fxy = new Func<double, double, double>((x, y) => x * x + 4 * y);
            Assert.Equal(1719, TwoDimensionalSimpsons(fxy, 11, 14, 7, 10, 50), 10);

            fxy = new Func<double, double, double>((x, y) => 9 * Exp(-x * x * x - y * y * y));
            Assert.Equal(5.86869, TwoDimensionalSimpsons(fxy, 0, 1, 0, 1, 8), 4);
        }

        [Fact]
        public void SimpsonsRuleExtendedFacts()
        {
            var f = new Func<double, double>(t => t * 2.0);
            Assert.Throws<Exception>(() => SimpsonsRuleExtended(f, 0, 7, 11));
            Assert.Equal(0, SimpsonsRuleExtended(f, 0, 0, 10));
            Assert.Equal(100, SimpsonsRuleExtended(f, 0, 10, 10));

            f = new Func<double, double>(t => t * t);
            Assert.Equal(333.3333333, SimpsonsRuleExtended(f, 0, 10, 10), 5);

        }

        [Fact]
        //https://en.wikipedia.org/wiki/Legendre_polynomials 
        public void LegendrePolynomialFacts()
        {
            Assert.Equal(1.0, LegendrePolynomial(7, 0));
            Assert.Equal(7.0, LegendrePolynomial(7, 1));
            Assert.Equal(0.5 * (3 * 7 * 7 - 1), LegendrePolynomial(7, 2));
            Assert.Equal(0.5 * (5 * 7 * 7 * 7 - 3 * 7), LegendrePolynomial(7, 3));
            Assert.Equal(0.125 * (35 * 7 * 7 * 7 * 7 - 30 * 7 * 7 + 3), LegendrePolynomial(7, 4));
        }

        [Fact]
        public void GuassLegendreFacts()
        {
            var f = new Func<double, double>(t => t * 2.0);
            Assert.Throws<Exception>(() => GaussLegendre(f, 0, 7, 25));
            Assert.Equal(0, GaussLegendre(f, 0, 0, 5), 10);
            Assert.Equal(100, GaussLegendre(f, 0, 10, 5), 10);

            f = new Func<double, double>(t => t * t);
            var fi = new Func<double, double>(t => t * t * t / 3.0);
            for (var n = 2; n<= 16; n++)
            {
                Assert.Equal(fi(10) - fi(0), GaussLegendre(f, 0, 10, n), 0);
            }
        }

        [Fact]
        public void GuassLegendre2dFacts()
        {
            var fxy = new Func<double, double, double>((x, y) => 2);
            Assert.Equal(12, TwoDimensionalGaussLegendre(fxy, 3, 6, 2, 4, 5), 10);

            fxy = new Func<double, double, double>((x, y) => x * x + 4 * y);
            Assert.Equal(1719, TwoDimensionalGaussLegendre(fxy, 11, 14, 7, 10, 5), 10);

            fxy = new Func<double, double, double>((x, y) => 9 * Exp(-x * x * x - y * y * y));
            Assert.Equal(5.86869, TwoDimensionalGaussLegendre(fxy, 0, 1, 0, 1, 5), 4);
        }
    }
}
