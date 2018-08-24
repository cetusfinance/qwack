using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options.Asians;

namespace Qwack.Models.Models
{
    public static class AssetProductEx
    {
        public static double PV(this AsianOption asianOption, IAssetFxModel model)
        {
            var fixingDates = asianOption.AverageStartDate.BusinessDaysInPeriod(asianOption.AverageEndDate, asianOption.FixingCalendar);
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(RollType.F, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            double fwd, fixedAvg=0;

            if (model.BuildDate < asianOption.AverageStartDate)
            {
                fwd = curve.GetAveragePriceForDates(asianOption.FixingDates.AddPeriod(RollType.F, asianOption.FixingCalendar, asianOption.SpotLag));
            }
            else
            {
                var fixingForToday = asianOption.AverageStartDate <= model.BuildDate &&
                        model.TryGetFixingDictionary(asianOption.AssetId, out var fixings) &&
                        fixings.TryGetValue(model.BuildDate, out var todayFixing);

                var alreadyFixed = asianOption.FixingDates.Where(d => d > model.BuildDate || (d == model.BuildDate && fixingForToday));
                var stillToFix = asianOption.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                if (alreadyFixed.Any())
                {
                    model.TryGetFixingDictionary(asianOption.AssetId, out var fixingDict);
                    fixedAvg = alreadyFixed.Select(d => fixingDict[d]).Average();
                }

                fwd = curve.GetAveragePriceForDates(stillToFix.AddPeriod(RollType.F, asianOption.FixingCalendar, asianOption.SpotLag));      
            }

            var volDate = asianOption.AverageStartDate.Average(asianOption.AverageEndDate);
            var volFwd = curve.GetPriceForDate(volDate);
            var sigma = model.GetVolSurface(asianOption.AssetId).GetVolForAbsoluteStrike(asianOption.Strike, volDate, volFwd);
            var discountCurve = model.FundingModel.Curves[asianOption.DiscountCurve];

            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            return TurnbullWakeman.PV(fwd, fixedAvg, sigma, asianOption.Strike, model.BuildDate, asianOption.AverageStartDate, asianOption.AverageEndDate, riskFree, asianOption.CallPut);
        }

        public static double PV(this AsianSwap asianSwap, IAssetFxModel model)
        {
            var priceCurve = model.GetPriceCurve(asianSwap.AssetId);
            var discountCurve = model.FundingModel.Curves[asianSwap.DiscountCurve];
            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
                asianSwap.AverageEndDate.AddPeriod(RollType.F, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
                asianSwap.PaymentDate;

            double avg;

            if (model.BuildDate < asianSwap.AverageStartDate)
            {
                avg = priceCurve.GetAveragePriceForDates(asianSwap.FixingDates.AddPeriod(RollType.F, asianSwap.FixingCalendar, asianSwap.SpotLag));
            }
            else
            {
                var fixingForToday = asianSwap.AverageStartDate <= model.BuildDate &&
                        model.TryGetFixingDictionary(asianSwap.AssetId, out var fixings) &&
                        fixings.TryGetValue(model.BuildDate, out var todayFixing);

                var alreadyFixed = asianSwap.FixingDates.Where(d => d > model.BuildDate || (d == model.BuildDate && fixingForToday));
                var stillToFix = asianSwap.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                double fixedSum = 0;
                if (alreadyFixed.Any())
                {
                    model.TryGetFixingDictionary(asianSwap.AssetId, out var fixingDict);
                    fixedSum = alreadyFixed.Select(d => fixingDict[d]).Sum();
                }
                var floatSum = priceCurve.GetAveragePriceForDates(stillToFix.AddPeriod(RollType.F, asianSwap.FixingCalendar, asianSwap.SpotLag)) * stillToFix.Length;
                avg = (fixedSum + floatSum) / asianSwap.FixingDates.Length;
            }

            var pv = avg - asianSwap.Strike;
            pv *= asianSwap.Direction == TradeDirection.Long ? 1.0 : -1.0;
            pv *= asianSwap.Notional;
            pv *= discountCurve.GetDf(priceCurve.BuildDate, payDate);

            return pv;
        }

        public static double PV(this AsianSwapStrip asianSwap, IAssetFxModel model)
        {
            return asianSwap.Swaplets.Sum(x => x.PV(model));
        }

        public static double PV(this Forward fwd, IAssetFxModel model)
        {
            var payDate = fwd.ExpiryDate.AddPeriod(RollType.F, fwd.PaymentCalendar, fwd.PaymentLag);
            if (model.BuildDate > payDate)
                return 0;

            double fwdPrice;
            if (fwd.ExpiryDate >= model.BuildDate)
            {
                if (fwd.ExpiryDate == model.BuildDate && 
                    model.TryGetFixingDictionary(fwd.AssetId, out var fixings) && 
                    fixings.TryGetValue(fwd.ExpiryDate, out var fixing))
                {
                    fwdPrice = fixing;
                }
                else
                {
                    var priceCurve = model.GetPriceCurve(fwd.AssetId);
                    fwdPrice = priceCurve.GetPriceForDate(fwd.ExpiryDate.AddPeriod(RollType.F, fwd.FixingCalendar, fwd.SpotLag));
                }
            }
            else
            {
                var fixingCurve = model.GetFixingDictionary(fwd.AssetId);
                fwdPrice = fixingCurve[fwd.ExpiryDate];
            }

            var discountCurve = model.FundingModel.Curves[fwd.DiscountCurve];
            var pv = fwdPrice - fwd.Strike;
            pv *= fwd.Direction == TradeDirection.Long ? 1.0 : -1.0;
            pv *= fwd.Notional;
            pv *= discountCurve.GetDf(model.BuildDate, payDate);

            return pv;
        }

        public static ICube PV(this Portfolio portfolio, IAssetFxModel model)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("PV", typeof(double));
            cube.Initialize(dataTypes);

            foreach(var ins in portfolio.Instruments)
            {
                double pv = 0;
                string tradeId = null;
                switch(ins)
                {
                    case AsianOption asianOption:
                        pv = asianOption.PV(model);
                        tradeId = asianOption.TradeId;
                        break;
                    case AsianSwap swap:
                        pv = swap.PV(model);
                        tradeId = swap.TradeId;
                        break;
                    case AsianSwapStrip swapStrip:
                        pv = swapStrip.PV(model);
                        tradeId = swapStrip.TradeId;
                        break;
                    case Forward fwd:
                        pv = fwd.PV(model);
                        tradeId = fwd.TradeId;
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }

                var row = new Dictionary<string, object>();
                row.Add("TradeId", tradeId);
                row.Add("PV", pv);
                cube.AddRow(row);
            }

            return cube;
        }
    }
}
