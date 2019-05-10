using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Curves;
using Xunit;
using Moq;
using Qwack.Core.Models;
using Qwack.Core.Basic;

namespace Qwack.Models.Tests
{
    public class AssetFxModelFacts
    {
        [Fact]
        public void FixingsVolsCurvesFacts()
        {
            var fModel = new Mock<IFundingModel>();
            var matrix = new Mock<IFxMatrix>();
            var pair = new FxPair();
            var dict = new Mock<IFixingDictionary>();
            var surface = new Mock<IVolSurface>();
            var surfaceFx = new Mock<IVolSurface>();
            var curve = new Mock<IPriceCurve>();
            curve.Setup(c => c.GetPriceForDate(DateTime.Today)).Returns(456.0);

            matrix.Setup(f => f.GetFxPair(It.IsAny<string>())).Returns(pair);
            fModel.Setup(f => f.GetFxRate(It.IsAny<DateTime>(), It.IsAny<Currency>(), It.IsAny<Currency>())).Returns(77.0);
            fModel.Setup(f => f.FxMatrix).Returns(matrix.Object);
            fModel.Setup(f => f.VolSurfaces).Returns(new Dictionary<string, IVolSurface> { { "bla/haa", surfaceFx.Object } });
            var sut = new AssetFxModel(DateTime.Today, fModel.Object);

            sut.AddPriceCurve("blah", curve.Object);
            sut.AddPriceCurves(new Dictionary<string, IPriceCurve> { { "blah2", curve.Object } });
            Assert.Same(curve.Object, sut.GetPriceCurve("blah"));
            Assert.Same(curve.Object, sut.GetPriceCurve("blah2"));

            sut.AddFixingDictionary("blah", dict.Object);
            sut.AddFixingDictionaries(new Dictionary<string, IFixingDictionary> { { "blah2", dict.Object } });
            Assert.Same(dict.Object, sut.GetFixingDictionary("blah"));
            Assert.Same(dict.Object, sut.GetFixingDictionary("blah2"));
            Assert.False(sut.TryGetFixingDictionary("wooo", out var flob));

            sut.AddVolSurface("blah", surface.Object);
            sut.AddVolSurfaces(new Dictionary<string, IVolSurface> { { "blah2", surface.Object } });
            Assert.Same(surface.Object, sut.GetVolSurface("blah"));
            Assert.Same(surface.Object, sut.GetVolSurface("blah2"));

            sut.GetVolForStrikeAndDate("blah", DateTime.Today, 123);
            surface.Verify(s => s.GetVolForAbsoluteStrike(123, DateTime.Today, 456), Times.Once);
            sut.GetVolForDeltaStrikeAndDate("blah", DateTime.Today, 123);
            surface.Verify(s => s.GetVolForDeltaStrike(123, DateTime.Today, 456), Times.Once);

            sut.GetAverageVolForStrikeAndDates("blah", new[] { DateTime.Today }, 123);
            surface.Verify(s => s.GetVolForAbsoluteStrike(123, DateTime.Today, 456), Times.Exactly(2));
            sut.GetAverageVolForMoneynessAndDates("blah", new[] { DateTime.Today }, 1.0);
            surface.Verify(s => s.GetVolForAbsoluteStrike(456, DateTime.Today, 456), Times.Exactly(2));

            sut.GetFxVolForStrikeAndDate("bla/haa", DateTime.Today, 123);
            surfaceFx.Verify(s => s.GetVolForAbsoluteStrike(123, DateTime.Today, 77), Times.Once);
            sut.GetFxVolForDeltaStrikeAndDate("bla/haa", DateTime.Today, 123);
            surfaceFx.Verify(s => s.GetVolForDeltaStrike(123, DateTime.Today, 77), Times.Once);

            sut.OverrideBuildDate(DateTime.MinValue);
            Assert.Equal(DateTime.MinValue, sut.BuildDate);
            sut.OverrideBuildDate(DateTime.Today);
        }
    }
}
