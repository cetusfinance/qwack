using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Models.Models;
using Qwack.Models;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Core.Tests.Instruments
{
    public class SyntheticCashAndCarryFacts
    {
        [Fact]
        public void SyntheticCashAndCarryFactA()
        {
            var eur = TestProviderHelper.CurrencyProvider.GetCurrency("EUR");

            var origin = DateTime.Parse("2023-11-20");
            var expA = DateTime.Parse("2023-12-18");
            var expB = DateTime.Parse("2024-12-16");
            var nominal = 10000.0;

            var x = new SyntheticCashAndCarry
            {
                NearLeg = new Forward
                {
                    AssetId = "EUA",
                    PaymentCurrency = eur,
                    Strike = 88,
                    ExpiryDate = expA,
                    Notional = nominal,
                },
                FarLeg = new Forward
                {
                    AssetId = "EUA",
                    PaymentCurrency = eur,
                    Strike = 92,
                    ExpiryDate = expB,
                    Notional = nominal,
                },
                FundingRate = 0.0425,
                FundingBasis = DayCountBasis.ACT360,
                DiscountCurve = "EUR.DISCO",
                FundingRateType = RateType.Linear
            };

            //strike is leg2 premium in leg1 units

            var discoCurve = new ConstantRateIrCurve(0.05, origin, "EUR.DISCO", eur);
            var fModel = new FundingModel(origin, new[] { discoCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            fxMatrix.Init(eur, origin, new Dictionary<Currency, double>(), new List<FxPair>(), new() { { eur, "EUR.DISCO" } });
            fModel.SetupFx(fxMatrix);

            var aModel = new AssetFxModel(origin, fModel);
            var pillars = new DateTime[] { origin, expA, expB };
            var prices = new double[] { 78, 90, 93 };
            var euaCurve = new BasicPriceCurve(origin, pillars, prices, PriceCurveType.Linear, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "EUA",
                Name = "EUA",
                SpotLag = 0.Bd(),
                SpotCalendar = TestProviderHelper.CalendarProvider.GetCalendar("WEEKENDS")
            };

            aModel.AddPriceCurve("EUA", euaCurve);

            //PV before start is just change in spread, PV'd
            var pv = x.PV(aModel, false);

            var expectedPVFunding = 0.0;
            var expectedFVSpread = ((92 - 88) - (93 - 90)) * 10000;
            var expectedPVSpread = expectedFVSpread * discoCurve.GetDf(origin, expB);
            Assert.Equal(expectedPVFunding + expectedPVSpread, pv, 8);

            //PV at end is just initial spread less funding
            aModel = new AssetFxModel(expB, fModel);
            aModel.AddPriceCurve("EUA", euaCurve);

            pv = x.PV(aModel, false);

            expectedPVFunding = -expA.CalculateYearFraction(expB, x.FundingBasis) * x.FundingRate * x.NearLeg.Notional * x.NearLeg.Strike;
            expectedPVSpread = (92 - 88) * 10000;
            Assert.Equal(expectedPVFunding + expectedPVSpread, pv, 8);

            //PV in the middle is a blend of accrued and spread move
            var oNew = DateTime.Parse("2024-06-03");
            aModel = new AssetFxModel(oNew, fModel);
            pillars = new DateTime[] { oNew, expB };
            prices = new double[] { 78, 93 };
            euaCurve = new BasicPriceCurve(origin, pillars, prices, PriceCurveType.Linear, TestProviderHelper.CurrencyProvider)
            {
                AssetId = "EUA",
                Name = "EUA",
                SpotLag = 0.Bd(),
                SpotCalendar = TestProviderHelper.CalendarProvider.GetCalendar("WEEKENDS")
            };
            aModel.AddPriceCurve("EUA", euaCurve);

            pv = x.PV(aModel, false);

            expectedPVFunding = -expA.CalculateYearFraction(oNew, x.FundingBasis) * x.FundingRate * x.NearLeg.Notional * x.NearLeg.Strike;
            expectedFVSpread = ((92 - 88) - (93 - 78)) * 10000;
            expectedPVSpread = expectedFVSpread * discoCurve.GetDf(oNew, expB);
            Assert.Equal(expectedPVFunding + expectedPVSpread, pv, 8);
        }
    }
}
