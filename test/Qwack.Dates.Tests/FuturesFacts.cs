using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Futures;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class FuturesFacts
    {
        [Fact]
        public void CheckCrudeIsFound()
        {
            var sut = TestProviderHelper.FutureSettingsProvider;
            Assert.True(sut.TryGet("CL", out var crude));
        }

        [Fact]
        public void CodeMappingsTest()
        {
            var sut = TestProviderHelper.FutureSettingsProvider;
            Assert.True(sut.TryGet("PA", out var palladium));

            Assert.NotNull(palladium.CodeConversions);
            Assert.NotNull(palladium.CodeConversions["IB"]);
            Assert.Equal("PA", palladium.CodeConversions["IB"]["PA"]);
        }

        [Fact]
        public void CodeConverter()
        {
            var sut = TestProviderHelper.FutureSettingsProvider;
            var bbgCode = "COZ9 Comdty";
            var ibCode = bbgCode.ConvertBbgToIB(sut);
            Assert.Equal("COILZ9", ibCode);

            bbgCode = "SCOZ0 Comdty";
            ibCode = bbgCode.ConvertBbgToIB(sut);
            Assert.Equal("SCIZ0", ibCode);
        }
    }
}
