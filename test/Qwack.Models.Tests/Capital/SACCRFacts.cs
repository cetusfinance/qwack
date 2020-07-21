using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Models.MCModels;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;
using System.Linq;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Models.Models;
using Qwack.Core.Instruments.Funding;
using Qwack.Transport.BasicTypes;
using Qwack.Models.Risk;
using Qwack.Core.Instruments.Credit;

namespace Qwack.Models.Tests.Capital
{
    public class SACCRFacts
    {

        [Fact]
        //https://www.moodysanalytics.com/-/media/whitepaper/2014/2014-20-05-standardized-approach-for-capitalizing-counterparty-credit-risk-exposures.pdf
        //https://www.bis.org/publ/bcbs279.pdf - examples at the end
        public void MoodysTest3()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurve = new ConstantRateIrCurve(0.0, origin, "zero", usd);
            var fm = new FundingModel(origin, new[] { zeroCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var model = new AssetFxModel(origin, fm);

            model.AddPriceCurve("WTI", new ConstantPriceCurve(100, origin, TestProviderHelper.CurrencyProvider));
            model.AddPriceCurve("Brent", new ConstantPriceCurve(100, origin, TestProviderHelper.CurrencyProvider));
            model.AddPriceCurve("Silver", new ConstantPriceCurve(10, origin, TestProviderHelper.CurrencyProvider));

            var tradeA = new Forward()
            {
                AssetId = "WTI",
                ExpiryDate = origin.AddDays(9 * 365/12.0),
                PaymentDate = origin.AddDays(9 * 365 / 12.0),
                Notional = 100000,
                Strike = 100.5,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeA",
                CommodityType = "Oil",
                AssetClass = SaCcrAssetClass.CommoOilGas
            };
            var tradeB = new Forward()
            {
                AssetId = "Brent",
                ExpiryDate = origin.AddDays(2 * 365),
                PaymentDate = origin.AddDays(2 * 365),
                Notional = -200000,
                Strike = 99.85,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeB",
                CommodityType = "Oil",
                AssetClass = SaCcrAssetClass.CommoOilGas
            };
            var tradeC = new Forward()
            {
                AssetId = "Silver",
                ExpiryDate = origin.AddDays(5 * 365),
                PaymentDate = origin.AddDays(5 * 365),
                Notional = 1000000,
                Strike = 9.9,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeC",
                CommodityType = "Precious",
                AssetClass = SaCcrAssetClass.CommoMetals
            };

            var pvA = tradeA.PV(model, false);
            var pvB = tradeB.PV(model, false);
            var pvC = tradeC.PV(model, false);

            var pf = new Portfolio() { Instruments = new List<IInstrument> { tradeA, tradeB, tradeC } };
            var pvPf = pf.PV(model);
            var rc = 20000;
            Assert.Equal(rc, pvPf.GetAllRows().Sum(x=>x.Value), 8);
            var epe = System.Math.Max(0, pvPf.SumOfAllRows);
            var ead = SaCcrHelper.SaCcrEad(pf, model, rc);
            Assert.Equal(5408608, ead, 0);
        }

        [Fact]
        public void MoodysTest1A()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var eur = TestProviderHelper.CurrencyProvider.GetCurrency("EUR");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurveUsd = new ConstantRateIrCurve(0.06, origin, "zeroUsd", usd);
            var zeroCurveEur = new ConstantRateIrCurve(0.06, origin, "zeroEur", eur);
            var fm = new FundingModel(origin, new[] { zeroCurveUsd, zeroCurveEur }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, origin, new Dictionary<Currency, double> { { eur, 1.0 } }, new List<FxPair> { new FxPair { Domestic = eur, Foreign = usd } }, new Dictionary<Currency, string> { { eur, "zeroEur" }, { usd, "zeroUsd" } });
            fm.SetupFx(fxMatrix);

            var usdLibor = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis=DayCountBasis.ACT360,
                FixingOffset=2.Bd(),
                ResetTenor=3.Months()
            };
            var euribor = new FloatRateIndex
            {
                Currency = eur,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };

            var model = new AssetFxModel(origin, fm);

            var trade1 = new IrSwap(origin, 10.Years(), usdLibor, 0.01, SwapPayReceiveType.Pay, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade2 = new IrSwap(origin, 4.Years(), usdLibor, 0.01, SwapPayReceiveType.Rec, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade3 = new EuropeanSwaption(origin, 11.Years(), euribor, 0.05, SwapPayReceiveType.Pay, "zeroEur", "zeroEur", origin.AddDays(360)) { Notional = 5e6 };

            var portfolio = new Portfolio { Instruments = new List<IInstrument> { trade1, trade2, trade3 } };
            var rc = 60000;

            var ead = SaCcrHelper.SaCcrEad(portfolio, model, rc);

            Assert.Equal(580138.75, ead, 2); //adjusted from example
            
        }

        [Fact]
        public void MoodysTest2()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var eur = TestProviderHelper.CurrencyProvider.GetCurrency("EUR");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurveUsd = new ConstantRateIrCurve(0.01, origin, "zeroUsd", usd);
            var zeroCurveEur = new ConstantRateIrCurve(0.01, origin, "zeroEur", eur);
            var fm = new FundingModel(origin, new[] { zeroCurveUsd, zeroCurveEur }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, origin, new Dictionary<Currency, double> { { eur, 1.0 } }, new List<FxPair> { new FxPair { Domestic = eur, Foreign = usd } }, new Dictionary<Currency, string> { { eur, "zeroEur" }, { usd, "zeroUsd" } });
            fm.SetupFx(fxMatrix);

            var usdLibor = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };
            var euribor = new FloatRateIndex
            {
                Currency = eur,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };

            var model = new AssetFxModel(origin, fm);

            var trade1 = new CDS
            {
                Currency = usd,
                OriginDate = origin,
                Tenor = 3.Years(),
                Notional = 10e6,
                ReferenceName = "Firm A",
                ReferenceRating = "AA"
            };
            var trade2 = new CDS
            {
                Currency = eur,
                OriginDate = origin,
                Tenor = 6.Years(),
                Notional = -10e6,
                ReferenceName = "Firm B",
                ReferenceRating = "BBB"
            };
            var trade3 = new CDS
            {
                Currency = usd,
                OriginDate = origin,
                Tenor = 5.Years(),
                Notional = 10e6,
                ReferenceName = "CDX IG 5y",
                ReferenceRating = "IG"
            };

            trade1.Init();
            trade2.Init();
            trade3.Init();

            var portfolio = new Portfolio { Instruments = new List<IInstrument> { trade1, trade2, trade3 } };
            var rc = -20000;

            var ead = SaCcrHelper.SaCcrEad(portfolio, model, rc);

            Assert.Equal(381526.13, ead, 2); //adjusted from example

        }

        [Fact]
        public void MoodysTest4()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var eur = TestProviderHelper.CurrencyProvider.GetCurrency("EUR");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurveUsd = new ConstantRateIrCurve(0.01, origin, "zeroUsd", usd);
            var zeroCurveEur = new ConstantRateIrCurve(0.01, origin, "zeroEur", eur);
            var fm = new FundingModel(origin, new[] { zeroCurveUsd, zeroCurveEur }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, origin, new Dictionary<Currency, double> { { eur, 1.0 } }, new List<FxPair> { new FxPair { Domestic = eur, Foreign = usd } }, new Dictionary<Currency, string> { { eur, "zeroEur" }, { usd, "zeroUsd" } });
            fm.SetupFx(fxMatrix);

            var usdLibor = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };
            var euribor = new FloatRateIndex
            {
                Currency = eur,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };

            var model = new AssetFxModel(origin, fm);

            var trade1 = new IrSwap(origin, 10.Years(), usdLibor, 0.01, SwapPayReceiveType.Pay, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade2 = new IrSwap(origin, 4.Years(), usdLibor, 0.01, SwapPayReceiveType.Rec, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade3 = new EuropeanSwaption(origin, 11.Years(), euribor, 0.0083, SwapPayReceiveType.Pay, "zeroEur", "zeroEur", origin.AddDays(360)) { Notional = 5e6 };

            var trade4 = new CDS
            {
                Currency = usd,
                OriginDate = origin,
                Tenor = 3.Years(),
                Notional = 10e6,
                ReferenceName = "Firm A",
                ReferenceRating = "AA"
            };
            var trade5 = new CDS
            {
                Currency = eur,
                OriginDate = origin,
                Tenor = 6.Years(),
                Notional = -10e6,
                ReferenceName = "Firm B",
                ReferenceRating = "BBB"
            };
            var trade6 = new CDS
            {
                Currency = usd,
                OriginDate = origin,
                Tenor = 5.Years(),
                Notional = 10e6,
                ReferenceName = "CDX IG 5y",
                ReferenceRating = "IG"
            };

            trade4.Init();
            trade5.Init();
            trade6.Init();

            var portfolio = new Portfolio { Instruments = new List<IInstrument> { trade1, trade2, trade3, trade4, trade5, trade6 } };
            var rc = 40000;

            var ead = SaCcrHelper.SaCcrEad(portfolio, model, rc);

            Assert.Equal(947855.25, ead, 2); //adjusted from example

        }

        [Fact]
        public void MoodysTest5()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var eur = TestProviderHelper.CurrencyProvider.GetCurrency("EUR");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurveUsd = new ConstantRateIrCurve(0.01, origin, "zeroUsd", usd);
            var zeroCurveEur = new ConstantRateIrCurve(0.01, origin, "zeroEur", eur);
            var fm = new FundingModel(origin, new[] { zeroCurveUsd, zeroCurveEur }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);
            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(usd, origin, new Dictionary<Currency, double> { { eur, 1.0 } }, new List<FxPair> { new FxPair { Domestic = eur, Foreign = usd } }, new Dictionary<Currency, string> { { eur, "zeroEur" }, { usd, "zeroUsd" } });
            fm.SetupFx(fxMatrix);

            var usdLibor = new FloatRateIndex
            {
                Currency = usd,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };
            var euribor = new FloatRateIndex
            {
                Currency = eur,
                DayCountBasis = DayCountBasis.ACT360,
                FixingOffset = 2.Bd(),
                ResetTenor = 3.Months()
            };

            var model = new AssetFxModel(origin, fm);

            model.AddPriceCurve("WTI", new ConstantPriceCurve(100, origin, TestProviderHelper.CurrencyProvider));
            model.AddPriceCurve("Brent", new ConstantPriceCurve(100, origin, TestProviderHelper.CurrencyProvider));
            model.AddPriceCurve("Silver", new ConstantPriceCurve(10, origin, TestProviderHelper.CurrencyProvider));

            var trade1 = new IrSwap(origin, 10.Years(), usdLibor, 0.01, SwapPayReceiveType.Pay, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade2 = new IrSwap(origin, 4.Years(), usdLibor, 0.01, SwapPayReceiveType.Rec, "zeroUsd", "zeroUsd") { Notional = 10e6 };
            var trade3 = new EuropeanSwaption(origin, 11.Years(), euribor, 0.0083, SwapPayReceiveType.Pay, "zeroEur", "zeroEur", origin.AddDays(360)) { Notional = 5e6 };

            var trade4 = new Forward()
            {
                AssetId = "WTI",
                ExpiryDate = origin.AddDays(9 * 365 / 12.0),
                PaymentDate = origin.AddDays(9 * 365 / 12.0),
                Notional = 100000,
                Strike = 100.5,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeA",
                CommodityType = "Oil",
                AssetClass = SaCcrAssetClass.CommoOilGas
            };
            var trade5 = new Forward()
            {
                AssetId = "Brent",
                ExpiryDate = origin.AddDays(2 * 365),
                PaymentDate = origin.AddDays(2 * 365),
                Notional = -200000,
                Strike = 99.85,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeB",
                CommodityType = "Oil",
                AssetClass = SaCcrAssetClass.CommoOilGas
            };
            var trade6 = new Forward()
            {
                AssetId = "Silver",
                ExpiryDate = origin.AddDays(5 * 365),
                PaymentDate = origin.AddDays(5 * 365),
                Notional = 1000000,
                Strike = 9.9,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeC",
                CommodityType = "Precious",
                AssetClass = SaCcrAssetClass.CommoMetals
            };

            var portfolio = new Portfolio { Instruments = new List<IInstrument> { trade1, trade2, trade3, trade4, trade5, trade6 } };
            var rc = 80000;

            var collateralSpec = new SaCcrCollateralSpec
            {
                Collateral = 200000,
                HasVm = true,
                MTA = 5000,
                Threshold = 0,
                NICA = 150000,
            };

            collateralSpec.SetMPOR(5); //weekly margining

            var ead = SaCcrHelper.SaCcrEad_Margined(portfolio, model, collateralSpec, rc);

            Assert.Equal(1875347.997, ead, 2); //adjusted from example

        }
    }
}



