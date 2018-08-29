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

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            var fwd = FloatAverage;
            var fixedAvg = FixedAverage;
            
            var volDate = asianOption.AverageStartDate.Average(asianOption.AverageEndDate);
            var volFwd = curve.GetPriceForDate(volDate);
            var sigma = model.GetVolSurface(asianOption.AssetId).GetVolForAbsoluteStrike(asianOption.Strike, volDate, volFwd);
            var discountCurve = model.FundingModel.Curves[asianOption.DiscountCurve];

            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            return TurnbullWakeman.PV(fwd, fixedAvg, sigma, asianOption.Strike, model.BuildDate, asianOption.AverageStartDate, asianOption.AverageEndDate, riskFree, asianOption.CallPut) * asianOption.Notional;
        }

        public static double PV(this AsianSwap asianSwap, IAssetFxModel model)
        {
            var discountCurve = model.FundingModel.Curves[asianSwap.DiscountCurve];
            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
                asianSwap.AverageEndDate.AddPeriod(RollType.F, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
                asianSwap.PaymentDate;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianSwap.GetAveragesForSwap(model);
            var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FloatCount + FixedCount);
            var pv = avg - asianSwap.Strike;
            pv *= asianSwap.Direction == TradeDirection.Long ? 1.0 : -1.0;
            pv *= asianSwap.Notional;
            pv *= discountCurve.GetDf(model.BuildDate, payDate);

            return pv;
        }

        public static (double FixedAverage, double FloatAverage, int FixedCount, int FloatCount) GetAveragesForSwap(this AsianSwap swap, IAssetFxModel model)
        {
            var fxDates = swap.FxFixingDates ?? swap.FixingDates;
            var priceCurve = model.GetPriceCurve(swap.AssetId);
            var fxFixingId = string.IsNullOrEmpty(swap.FxFixingId) ? $"{priceCurve.Currency}/{swap.PaymentCurrency}" : swap.FxFixingId;

            if (swap.PaymentCurrency == priceCurve.Currency || swap.FxConversionType == FxConversionType.AverageThenConvert)
            {
                var fxAverage = 1.0;

                if (model.BuildDate < swap.FixingDates.First())
                {
                    var avg = priceCurve.GetAveragePriceForDates(swap.FixingDates.AddPeriod(RollType.F, swap.FixingCalendar, swap.SpotLag));

                    if (swap.FxConversionType == FxConversionType.AverageThenConvert)
                    {
                        fxAverage = model.FundingModel.GetFxAverage(fxDates, priceCurve.Currency, swap.PaymentCurrency);
                        avg *= fxAverage;
                    }

                    return (FixedAverage: 0, FloatAverage: avg, FixedCount:0, FloatCount:swap.FixingDates.Length);
                }
                else
                {
                    var fixingForToday = swap.AverageStartDate <= model.BuildDate &&
                            model.TryGetFixingDictionary(swap.AssetId, out var fixings) &&
                            fixings.TryGetValue(model.BuildDate, out var todayFixing);

                    var alreadyFixed = swap.FixingDates.Where(d => d < model.BuildDate || (d == model.BuildDate && fixingForToday));
                    var stillToFix = swap.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                    double fixedAvg = 0;
                    if (alreadyFixed.Any())
                    {
                        model.TryGetFixingDictionary(swap.AssetId, out var fixingDict);
                        fixedAvg = alreadyFixed.Select(d => fixingDict[d]).Average();
                    }
                    var floatAvg = priceCurve.GetAveragePriceForDates(stillToFix.AddPeriod(RollType.F, swap.FixingCalendar, swap.SpotLag));
                    
                    if (swap.FxConversionType == FxConversionType.AverageThenConvert)
                    {
                        var fixingForTodayFx = fxDates.First() <= model.BuildDate &&
                            model.TryGetFixingDictionary(fxFixingId, out var fxFixings) &&
                            fxFixings.TryGetValue(model.BuildDate, out var todayFxFixing);

                        var alreadyFixedFx = fxDates.Where(d => d < model.BuildDate || (d == model.BuildDate && fixingForTodayFx));
                        var stillToFixFx = fxDates.Where(d => !alreadyFixedFx.Contains(d)).ToArray();

                        double fixedFxAvg = 0;
                        if (alreadyFixedFx.Any())
                        {
                            model.TryGetFixingDictionary(fxFixingId, out var fxFixingDict);
                            fixedFxAvg = alreadyFixedFx.Select(d => fxFixingDict[d]).Average();
                        }
                        var floatFxAvg = model.FundingModel.GetFxAverage(stillToFixFx, priceCurve.Currency, swap.PaymentCurrency);

                        floatAvg *= floatFxAvg;
                        fixedAvg *= fixedFxAvg;
                    }

                    return (FixedAverage: fixedAvg, FloatAverage: floatAvg, FixedCount: alreadyFixed.Count(), FloatCount: stillToFix.Count());
                }
            }
            else // convert then average...
            {
                var fixingForTodayAsset = swap.AverageStartDate <= model.BuildDate &&
                           model.TryGetFixingDictionary(swap.AssetId, out var fixings) &&
                           fixings.TryGetValue(model.BuildDate, out var todayFixing);
                var fixingForTodayFx = fxDates.First() <= model.BuildDate &&
                          model.TryGetFixingDictionary(fxFixingId, out var fxFixings) &&
                          fxFixings.TryGetValue(model.BuildDate, out var todayFxFixing);

                var fixingForToday = fixingForTodayAsset && fixingForTodayFx;

                var alreadyFixed = swap.FixingDates.Where(d => d < model.BuildDate || (d == model.BuildDate && fixingForToday));
                var stillToFix = swap.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                double fixedAvg = 0;
                if (alreadyFixed.Any())
                {
                    model.TryGetFixingDictionary(swap.AssetId, out var fixingDict);
                    var assetFixings = alreadyFixed.Select(d => fixingDict[d]);
                    model.TryGetFixingDictionary(fxFixingId, out var fxFixingDict);
                    var fxFixingsA = alreadyFixed.Select(d => fxFixingDict[d]).ToArray();
                    fixedAvg = assetFixings.Select((x, ix) => x * fxFixingsA[ix]).Average();
                }

                var floatAsset = stillToFix.AddPeriod(RollType.F, swap.FixingCalendar, swap.SpotLag)
                    .Select(d => priceCurve.GetPriceForDate(d));
                var floatFx = model.FundingModel.GetFxRates(stillToFix, priceCurve.Currency, swap.PaymentCurrency).ToArray();
                var floatCompoAvg = floatAsset.Select((x, ix) => x * floatFx[ix]).Average();

                return (FixedAverage: fixedAvg, FloatAverage: floatCompoAvg, FixedCount: alreadyFixed.Count(), FloatCount: stillToFix.Count());
            }
        }

        public static double PV(this AsianSwapStrip asianSwap, IAssetFxModel model) => asianSwap.Swaplets.Sum(x => x.PV(model));

        public static double PV(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().PV(model);

        public static ICube PV(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency=null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("Currency", typeof(string));
            cube.Initialize(dataTypes);

            foreach(var ins in portfolio.Instruments)
            {
                var pv = 0.0;
                var fxRate = 1.0;
                string tradeId = null;
                var ccy = reportingCurrency?.ToString();
                switch (ins)
                {
                    case AsianOption asianOption:
                        pv = asianOption.PV(model);
                        tradeId = asianOption.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, asianOption.PaymentCurrency);
                        else
                            ccy = asianOption.PaymentCurrency.ToString();
                        break;
                    case AsianSwap swap:
                        pv = swap.PV(model);
                        tradeId = swap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swap.PaymentCurrency);
                        else
                            ccy = swap.PaymentCurrency.ToString();
                        break;
                    case AsianSwapStrip swapStrip:
                        pv = swapStrip.PV(model);
                        tradeId = swapStrip.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swapStrip.Swaplets.First().PaymentCurrency);
                        else
                            ccy = swapStrip.Swaplets.First().PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        pv = fwd.PV(model);
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }


                var row = new Dictionary<string, object>();
                row.Add("TradeId", tradeId);
                row.Add("Currency", ccy);
                cube.AddRow(row,pv/fxRate);
            }

            return cube;
        }

        public static ICube AssetDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("AssetId", typeof(string));
            dataTypes.Add("PointLabel", typeof(string));
            dataTypes.Add("Metric", typeof(string));
            cube.Initialize(dataTypes);

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);

                var pvCube = portfolio.PV(model, curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex("TradeId");

                var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize);

                foreach (var bCurve in bumpedCurves)
                {
                    var newModel = model.Clone();
                    newModel.AddPriceCurve(curveName, bCurve.Value);
                    var bumpedPVCube = portfolio.PV(newModel, curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize;
                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>();
                            row.Add("TradeId", bumpedRows[i].MetaData[tidIx]);
                            row.Add("AssetId", curveName);
                            row.Add("PointLabel", bCurve.Key);
                            row.Add("Metric", "Delta");
                            cube.AddRow(row, delta);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube FxDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("FxPair", typeof(string));
            dataTypes.Add("Metric", typeof(string));
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model);
            var pvRows = pvCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex("TradeId");

            var domCcy = model.FundingModel.FxMatrix.BaseCurrency;

            foreach (var currency in model.FundingModel.FxMatrix.SpotRates.Keys)
            {
                var fxPair = $"{currency}/{domCcy}";
                var spot = model.FundingModel.FxMatrix.SpotRates[currency];
                var bumpedSpot = spot + bumpSize;
                var newModel = model.Clone();
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var bumpedPVCube = portfolio.PV(newModel);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");
                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize;
                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>();
                        row.Add("TradeId", bumpedRows[i].MetaData[tidIx]);
                        row.Add("FxPair", fxPair);
                        row.Add("Metric", "FxSpotDelta");
                        cube.AddRow(row, delta);
                    }
                }

            }
            return cube;
        }

        public static ICube AssetDeltaGamma(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("AssetId", typeof(string));
            dataTypes.Add("PointLabel", typeof(string));
            dataTypes.Add("Metric", typeof(string));
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex("TradeId");

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);
                var bumpedUpCurves = curveObj.GetDeltaScenarios(bumpSize);
                var bumpedDownCurves = curveObj.GetDeltaScenarios(-bumpSize);
                foreach (var bUpCurve in bumpedUpCurves)
                {
                    var newModelUp = model.Clone();
                    var newModelDown = model.Clone();
                    newModelUp.AddPriceCurve(curveName, bUpCurve.Value);
                    newModelDown.AddPriceCurve(curveName, bumpedDownCurves[bUpCurve.Key]);
                    var bumpedUpPVCube = portfolio.PV(newModelUp);
                    var bumpedDownPVCube = portfolio.PV(newModelDown);
                    var bumpedUpRows = bumpedUpPVCube.GetAllRows();
                    var bumpedDownRows = bumpedDownPVCube.GetAllRows();

                    if (bumpedUpRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedUpRows.Length; i++)
                    {
                        var deltaUp = ((double)bumpedUpRows[i].Value - (double)pvRows[i].Value) / bumpSize;
                        var deltaDown = ((double)pvRows[i].Value - (double)bumpedDownRows[i].Value ) / bumpSize;
                        var gamma = (deltaUp - deltaDown) / bumpSize;
                        var delta = 0.5 * (deltaUp + deltaDown);

                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>();
                            row.Add("TradeId", bumpedUpRows[i].MetaData[tidIx]);
                            row.Add("AssetId", curveName);
                            row.Add("PointLabel", bUpCurve.Key);
                            row.Add("Metric", "Delta");
                            cube.AddRow(row, delta);

                            var rowG = new Dictionary<string, object>();
                            rowG.Add("TradeId", bumpedUpRows[i].MetaData[tidIx]);
                            rowG.Add("AssetId", curveName);
                            rowG.Add("PointLabel", bUpCurve.Key);
                            rowG.Add("Metric", "Gamma");
                            cube.AddRow(rowG, gamma);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube AssetVega(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>();
            dataTypes.Add("TradeId", typeof(string));
            dataTypes.Add("AssetId", typeof(string));
            dataTypes.Add("PointLabel", typeof(string));
            dataTypes.Add("Metric", typeof(string));
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex("TradeId");

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var volObj = model.GetVolSurface(surfaceName);
                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize);
                foreach (var bCurve in bumpedSurfaces)
                {
                    var newModel = model.Clone();
                    newModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPVCube = portfolio.PV(newModel);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = ((double)bumpedRows[i].Value- (double)pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>();
                            row.Add("TradeId", bumpedRows[i].MetaData[tidIx]);
                            row.Add("AssetId", surfaceName);
                            row.Add("PointLabel", bCurve.Key);
                            row.Add("Metric", "Vega");
                            cube.AddRow(row,vega);
                        }
                    }
                }
            }
            return cube;
        }
    }
}
