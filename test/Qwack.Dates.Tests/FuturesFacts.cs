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

        [Fact]
        public void ExtendedDateCodesTestLC()
        {
            var nextCode = "LCH5 Comdty";
            var startDate = new DateTime(2025, 01, 01);
            var sut = TestProviderHelper.FutureSettingsProvider;
            FutureCode fs = new(nextCode, startDate.Year - 1, sut);

            nextCode = fs.GetNextCode(false);
            FutureCode fc = new(nextCode, startDate.Year - 1, sut);

            var expiry = fc.GetExpiry();
        }

        [Fact]
        public void ExtendedDateCodesTestLH()
        {
            var nextCode = "LHH5 Comdty";
            var startDate = new DateTime(2025, 01, 01);
            var sut = TestProviderHelper.FutureSettingsProvider;
            FutureCode fs = new(nextCode, startDate.Year - 1, sut);

            nextCode = fs.GetNextCode(false);
            FutureCode fc = new(nextCode, startDate.Year - 1, sut);

            var expiry = fc.GetExpiry();
        }

        [Fact]
        public void ExtendedDateCodesTestLH2()
        {
            var nextCode = "LH";
            var startDate = new DateTime(2023, 01, 01);
            var sut = TestProviderHelper.FutureSettingsProvider;
            FutureCode fs = new(nextCode, sut);
            var fm = fs.GetFrontMonth(startDate, false);
        }

    

    }
}
