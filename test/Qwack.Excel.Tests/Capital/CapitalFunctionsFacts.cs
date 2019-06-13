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

namespace Qwack.Excel.Tests.Capital
{
    public class CapitalFunctionsFacts
    {
        [Fact]
        public void ComputeEADFacts()
        {
            var curve = new PriceCurve(DateTime.MinValue, new[] { DateTime.MinValue }, new[] { 0.0 }, PriceCurveType.Flat, ContainerStores.CurrencyProvider)
            { Currency = ContainerStores.CurrencyProvider.GetCurrency("ZAR") };
            var moqModel = new Mock<IAssetFxModel>();
            moqModel.Setup(m => m.GetPriceCurve("fakeAsset",null)).Returns(curve);
            moqModel.Setup(m => m.VanillaModel).Returns(moqModel.Object);
            moqModel.Setup(m => m.Rebuild(It.IsAny<IAssetFxModel>(),It.IsAny<Portfolio>())).Returns(moqModel.Object);
            var cube = new ResultCube();
            cube.Initialize(new Dictionary<string, Type>() { { "xxx", typeof(string) } });
            moqModel.Setup(m => m.PV(It.IsAny<Currency>())).Returns(cube);
            var swap = new AsianSwap()
            {
                AssetId = "fakeAsset",
                PaymentCurrency = ContainerStores.CurrencyProvider.GetCurrency("ZAR"),
                FixingDates = new[] { DateTime.MinValue }
            };
            var pf = new Portfolio { Instruments = new List<IInstrument>() { swap } };

            ContainerStores.GetObjectCache<Portfolio>().PutObject("moqPf", new SessionItem<Portfolio>() { Name = "moqPf", Value = pf });
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("moqModel", new SessionItem<IAssetFxModel>() { Name = "moqModel", Value = moqModel.Object });

            Assert.Equal("Could not find portfolio or trade with name blash", CapitalFunctions.ComputeEAD("blash", "frah", "ZAR", null));
            Assert.Equal("Model frah not found", CapitalFunctions.ComputeEAD("moqPf", "frah", "ZAR", null));
            Assert.Equal(0.0, CapitalFunctions.ComputeEAD("moqPf", "moqModel", "ZAR", new object[,] { { "fakeAsset", "woooh"} }));
        }

        [Fact]
        public void ComputeCVAFacts()
        {
            var hz = new HazzardCurve(DateTime.Today, Qwack.Dates.DayCountBasis.Act365F, new DummyPointInterpolator(0.0));
            var disco = new FlatIrCurve(0.0, ContainerStores.CurrencyProvider.GetCurrency("ZAR"), "disco");

            ContainerStores.GetObjectCache<HazzardCurve>().PutObject("hz", new SessionItem<HazzardCurve>() { Name = "hz", Value = hz });
            ContainerStores.GetObjectCache<IIrCurve>().PutObject("disco", new SessionItem<IIrCurve>() { Name = "disco", Value = disco});

            Assert.Equal("Hazzard curve blash not found", CapitalFunctions.ComputeCVA("blash", DateTime.Today, "woo", null));
            Assert.Equal("Discount curve woo not found", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "woo", null));
            Assert.Equal("Expected Nx2 array for EPE", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", new object[1,1]));
            Assert.Equal("EPE profile must be cube reference or Nx2 array", CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", 7));

            Assert.Equal(0.0, CapitalFunctions.ComputeCVA("hz", DateTime.Today, "disco", new object[1, 2] { { DateTime.Today,0.0 } }));
        }
    }
}
