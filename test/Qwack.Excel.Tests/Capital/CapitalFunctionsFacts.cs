using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Excel.Utils;
using Qwack.Excel.Capital;
using static ExcelDna.Integration.ExcelMissing;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Curves;
using Qwack.Core.Cubes;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Excel.Tests.Capital
{
    public class CapitalFunctionsFacts
    {
        [Fact]
        public void ComputeCVAFacts()
        {
            var hz = new HazzardCurve(DateTime.Today, DayCountBasis.Act365F, new DummyPointInterpolator(0.0));
            var disco = new FlatIrCurve(0.0, ContainerStores.CurrencyProvider.GetCurrency("ZAR"), "disco");

            ContainerStores.GetObjectCache<HazzardCurve>().PutObject("hz", new SessionItem<HazzardCurve>() { Name = "hz", Value = hz });
            ContainerStores.GetObjectCache<IIrCurve>().PutObject("disco", new SessionItem<IIrCurve>() { Name = "disco", Value = disco});

            Assert.Equal("Hazzard curve blash not found", CapitalFunctions.ComputeCVA("blash", DateTime.Today, "woo", null, 0.0));
            Assert.Equal("Discount curve woo not found", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "woo", null, 0.0));
            Assert.Equal("Expected Nx2 array for EPE", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", new object[1,1], 0.0));
            Assert.Equal("EPE profile must be cube reference or Nx2 array", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", 7, 0.0));

            Assert.Equal(0.0, CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", new object[1, 2] { { DateTime.Today,0.0 } }, 0.0));
        }
    }
}
