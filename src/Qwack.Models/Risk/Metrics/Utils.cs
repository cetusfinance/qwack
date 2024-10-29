using System;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Models.Risk.Metrics
{
    public static class Utils
    {
        public static double GetUsdDF(IAssetFxModel model, BasicPriceCurve priceCurve, DateTime fwdDate)
        {
            var colSpec = priceCurve.CollateralSpec;
            var ccy = priceCurve.Currency;
            var disccurve = model.FundingModel.GetCurveByCCyAndSpec(ccy, colSpec);
            return disccurve.GetDf(model.BuildDate, fwdDate);
        }

        public static DateTime NextThirdWeds(DateTime date)
        {
            var w3 = date.ThirdWednesday();
            if (date > w3)
                return date.AddMonths(1).ThirdWednesday();
            else
                return w3;
        }
    }
}
