using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap
    {
        public OptionType CallPut { get; set; }

        public new double PV(IAssetFxModel model)
        {
            var fixingDates = AverageStartDate.BusinessDaysInPeriod(AverageEndDate, FixingCalendar);
            var curve = model.GetPriceCurve(AssetId);
            var fwd = curve.GetAveragePriceForDates(fixingDates.ToArray());
            var fixings = 0.0;
            var volDate = AverageStartDate.Average(AverageEndDate);
            var volFwd = curve.GetPriceForDate(volDate);
            var sigma = model.GetVolSurface(AssetId).GetVolForAbsoluteStrike(Strike, volDate, volFwd);
            var riskFree = 0.0;
            return 0;// TurnbullWakeman.PV(fwd, fixings, sigma, Strike, model.BuildDate, AverageStartDate, AverageEndDate, riskFree, CallPut);
        }


    }
}
