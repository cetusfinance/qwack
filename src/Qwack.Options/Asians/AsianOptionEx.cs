using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Options.Asians
{
    public static class AsianOptionEx
    {
        public static double PV(this AssetFxModel model, AsianOption option)
        {
            var fixingDates = option.AverageStartDate.BusinessDaysInPeriod(option.AverageEndDate, option.FixingCalendar);
            var curve = model.GetPriceCurve(option.AssetId);
            var fwd = curve.GetAveragePriceForDates(fixingDates.ToArray());
            var fixings = 0.0;
            var volDate = option.AverageStartDate.Average(option.AverageEndDate);
            var volFwd = curve.GetPriceForDate(volDate);
            var sigma = model.GetVolSurface(option.AssetId).GetVolForAbsoluteStrike(option.Strike,volDate,volFwd);
            var riskFree = 0.0;
            return TurnbullWakeman.PV(fwd, fixings, sigma, option.Strike, model.BuildDate, option.AverageStartDate, option.AverageEndDate, riskFree, option.CallPut);
        }
    }
}
