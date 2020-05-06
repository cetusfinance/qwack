using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Serialization.Test.SpecificTypes
{
    public class RiskyFlyVolSurfaceFacts
    {
        [Fact(Skip="TimToFix")]
        
        public void RiskyFlySerialization()
        {
            var origin = new DateTime(2017, 02, 07);
            var atms = new double[] { 0.3, 0.32, 0.34 };
            var fwds = new double[] { 100, 102, 110 };
            var maturities = new DateTime[] { new DateTime(2017, 04, 06), new DateTime(2017, 06, 07), new DateTime(2017, 08, 07) };
            var wingDeltas = new[] { 0.1, 0.25 };
            var riskies = new[] { new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 }, new[] { 0.025, 0.015 } };
            var flies = new[] { new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 }, new[] { 0.0025, 0.0015 } };
            var surface = new RiskyFlySurface(
                origin, atms, maturities, wingDeltas, riskies, flies, fwds, WingQuoteType.Simple,
                AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear,
                Interpolator1DType.LinearInVariance);

            Assert.Equal(atms[1], surface.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]));

            var binSer = new BinarySerializer();
            binSer.PrepareObjectGraph(surface);
            var span = binSer.SerializeObjectGraph();

            var binDeser = new BinaryDeserializer();
            var surface2 = (ObjectWithLists)binDeser.DeserializeObjectGraph(span);

            Assert.Equal(atms[1], surface.GetVolForDeltaStrike(0.5, maturities[1], fwds[1]));
        }
    }
}
