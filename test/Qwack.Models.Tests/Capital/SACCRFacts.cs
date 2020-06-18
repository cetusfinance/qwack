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

namespace Qwack.Models.Tests.Capital
{
    public class SACCRFacts
    {

        [Fact]
        //https://www.moodysanalytics.com/-/media/whitepaper/2014/2014-20-05-standardized-approach-for-capitalizing-counterparty-credit-risk-exposures.pdf
        //Example netting set #3
        public void MoodysTest1()
        {
            var usd = TestProviderHelper.CurrencyProvider.GetCurrency("USD");
            var origin = DateTime.Parse("2019-02-05");
            var zeroCurve = new ConstantRateIrCurve(0.0, origin, "zero", usd);
            var fm = new FundingModel(origin, new[] { zeroCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var model = new AssetFxModel(origin, fm);

            model.AddPriceCurve("Crude", new ConstantPriceCurve(100, origin, TestProviderHelper.CurrencyProvider));
            model.AddPriceCurve("Silver", new ConstantPriceCurve(10, origin, TestProviderHelper.CurrencyProvider));

            var assetToSetMap = new Dictionary<string, string>
            {
                {"Crude","Energy" },
                {"Silver","Metals" },
            };

            var tradeA = new Forward()
            {
                AssetId = "Crude",
                ExpiryDate = origin.AddDays(9 * 365/12.0),
                PaymentDate = origin.AddDays(9 * 365 / 12.0),
                Notional = 100000,
                Strike = 100.5,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeA"
            };
            var tradeB = new Forward()
            {
                AssetId = "Crude",
                ExpiryDate = origin.AddDays(2 * 365),
                PaymentDate = origin.AddDays(2 * 365),
                Notional = -200000,
                Strike = 99.85,
                PaymentCurrency = usd,
                DiscountCurve = "zero",
                TradeId = "tradeB"
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
                TradeId = "tradeC"
            };

            var pvA = tradeA.PV(model, false);
            var pvB = tradeB.PV(model, false);
            var pvC = tradeC.PV(model, false);

            var pf = new Portfolio() { Instruments = new List<IInstrument> { tradeA, tradeB, tradeC } };
            var pvPf = pf.PV(model);
            Assert.Equal(20000.0, pvPf.GetAllRows().Sum(x=>x.Value), 8);
            var epe = System.Math.Max(0, pvPf.SumOfAllRows);
            var ead = pf.SaCcrEAD(epe, model, usd, assetToSetMap);
            Assert.Equal(5408608, ead, 0);
        }


    }
}



