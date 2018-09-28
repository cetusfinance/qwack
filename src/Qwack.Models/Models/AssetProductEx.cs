using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Descriptors;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options.Asians;

namespace Qwack.Models.Models
{
    public static class AssetProductEx
    {
        private static bool _useVarianceAverage = false;

        public static double PV(this AsianOption asianOption, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            if (payDate < model.BuildDate)
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            var fwd = FloatAverage;
            var fixedAvg = FixedAverage;

            if (asianOption.FixingDates.Last() <= model.BuildDate)
            {
                var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FixedCount + FloatCount);
                return asianOption.Notional * (asianOption.CallPut == OptionType.Call ? System.Math.Max(0, avg - asianOption.Strike) : System.Math.Max(0, asianOption.Strike - avg));
            }

            var surface = model.GetVolSurface(asianOption.AssetId);
            var isCompo = asianOption.PaymentCurrency != curve.Currency; //should reference vol surface, not curve

            double sigma;
            var volDate = model.BuildDate.Max(asianOption.AverageStartDate).Average(asianOption.AverageEndDate);

            var adjustedStrike = asianOption.Strike;
            if (asianOption.AverageStartDate < model.BuildDate)
            {
                var tAvgStart = model.BuildDate.CalculateYearFraction(asianOption.AverageStartDate, DayCountBasis.Act365F);
                var tExpiry = model.BuildDate.CalculateYearFraction(asianOption.AverageEndDate, DayCountBasis.Act365F);
                var t2 = tExpiry - tAvgStart;
                adjustedStrike = asianOption.Strike * t2 / tExpiry - FixedAverage * (t2 - tExpiry) / tExpiry;
            }

            if (_useVarianceAverage)
            {
                var toFix = asianOption.FixingDates.Where(x => x > model.BuildDate).ToArray();
                var moneyness = adjustedStrike / FloatAverage;
                sigma = model.GetAverageVolForMoneynessAndDates(asianOption.AssetId, toFix, moneyness);
            }
            else
            {
                var volFwd = curve.GetPriceForDate(volDate);
                sigma = surface.GetVolForAbsoluteStrike(adjustedStrike, volDate, volFwd);
            }


            if (isCompo)
            {
                var fxId = $"{curve.Currency.Ccy}/{asianOption.PaymentCurrency.Ccy}";
                var fxVolFwd = model.FundingModel.GetFxRate(volDate, curve.Currency, asianOption.PaymentCurrency);
                var fxVol = model.FundingModel.VolSurfaces[fxId].GetVolForDeltaStrike(0.5, volDate, fxVolFwd);
                var correl = model.CorrelationMatrix.GetCorrelation(fxId, asianOption.AssetId);
                sigma = System.Math.Sqrt(sigma * sigma + fxVol * fxVol + 2 * correl * fxVol * sigma);
            }

            var discountCurve = model.FundingModel.Curves[asianOption.DiscountCurve];

            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            return TurnbullWakeman.PV(fwd, fixedAvg, sigma, asianOption.Strike, model.BuildDate, asianOption.AverageStartDate, asianOption.AverageEndDate, riskFree, asianOption.CallPut) * asianOption.Notional;
        }

        public static double PV(this AsianSwap asianSwap, IAssetFxModel model)
        {
            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
                asianSwap.AverageEndDate.AddPeriod(asianSwap.PaymentLagRollType, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
                asianSwap.PaymentDate;

            if (payDate < model.BuildDate)
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianSwap.GetAveragesForSwap(model);
            var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FloatCount + FixedCount);
            var pv = avg - asianSwap.Strike;
            pv *= asianSwap.Direction == TradeDirection.Long ? 1.0 : -1.0;
            pv *= asianSwap.Notional;
            pv *= model.FundingModel.GetDf(asianSwap.DiscountCurve, model.BuildDate, payDate);

            return pv;
        }

        public static string GetFxFixingId(this AsianSwap swap, string curveCcy) => string.IsNullOrEmpty(swap.FxFixingId) ? $"{curveCcy}{swap.PaymentCurrency.Ccy}" : swap.FxFixingId;

        public static (double FixedAverage, double FloatAverage, int FixedCount, int FloatCount) GetAveragesForSwap(this AsianSwap swap, IAssetFxModel model)
        {
            var fxDates = swap.FxFixingDates ?? swap.FixingDates;
            var priceCurve = model.GetPriceCurve(swap.AssetId);
            var fxFixingId = swap.GetFxFixingId(priceCurve.Currency.Ccy);
            var assetFixingId = swap.AssetFixingId ?? swap.AssetId;

            if (swap.PaymentCurrency == priceCurve.Currency || swap.FxConversionType == FxConversionType.AverageThenConvert)
            {
                var fxAverage = 1.0;

                if (model.BuildDate < swap.FixingDates.First())
                {
                    var avg = priceCurve.GetAveragePriceForDates(swap.FixingDates.AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag));

                    if (swap.FxConversionType == FxConversionType.AverageThenConvert)
                    {
                        fxAverage = model.FundingModel.GetFxAverage(fxDates, priceCurve.Currency, swap.PaymentCurrency);
                        avg *= fxAverage;
                    }

                    return (FixedAverage: 0, FloatAverage: avg, FixedCount: 0, FloatCount: swap.FixingDates.Length);
                }
                else
                {
                    var fixingForToday = swap.AverageStartDate <= model.BuildDate &&
                            model.TryGetFixingDictionary(assetFixingId, out var fixings) &&
                            fixings.TryGetValue(model.BuildDate, out var todayFixing);

                    var alreadyFixed = swap.FixingDates.Where(d => d < model.BuildDate || (d == model.BuildDate && fixingForToday));
                    var stillToFix = swap.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                    double fixedAvg = 0;
                    if (alreadyFixed.Any())
                    {
                        model.TryGetFixingDictionary(assetFixingId, out var fixingDict);
                        if (fixingDict == null)
                            throw new Exception($"Fixing dictionary not found for asset fixing id {assetFixingId}");

                        fixedAvg = alreadyFixed.Select(d => fixingDict[d]).Average();
                    }
                    var floatAvg = stillToFix.Any() ? priceCurve.GetAveragePriceForDates(stillToFix.AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag)) : 0.0;

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
                            if (fxFixingDict == null)
                                throw new Exception($"Fx Fixing dictionary not found for asset fixing id {fxFixingId}");

                            fixedFxAvg = alreadyFixedFx.Select(d => fxFixingDict[d]).Average();
                        }
                        var floatFxAvg = stillToFixFx.Any() ? model.FundingModel.GetFxAverage(stillToFixFx, priceCurve.Currency, swap.PaymentCurrency) : 0.0;

                        floatAvg *= floatFxAvg;
                        fixedAvg *= fixedFxAvg;
                    }

                    return (FixedAverage: fixedAvg, FloatAverage: floatAvg, FixedCount: alreadyFixed.Count(), FloatCount: stillToFix.Count());
                }
            }
            else // convert then average...
            {
                var fixingForTodayAsset = swap.AverageStartDate <= model.BuildDate &&
                           model.TryGetFixingDictionary(assetFixingId, out var fixings) &&
                           fixings.TryGetValue(model.BuildDate, out var todayFixing);
                var fixingForTodayFx = fxDates.First() <= model.BuildDate &&
                          model.TryGetFixingDictionary(fxFixingId, out var fxFixings) &&
                          fxFixings.TryGetValue(model.BuildDate, out var todayFxFixing);

                var fixingForToday = fixingForTodayAsset && fixingForTodayFx;

                var alreadyFixed = swap.FixingDates.Where(d => d < model.BuildDate || (d == model.BuildDate && fixingForToday)).ToArray();
                var stillToFix = swap.FixingDates.Where(d => !alreadyFixed.Contains(d)).ToArray();

                double fixedAvg = 0;
                if (alreadyFixed.Any())
                {
                    model.TryGetFixingDictionary(assetFixingId, out var fixingDict);
                    if (fixingDict == null)
                        throw new Exception($"Fixing dictionary not found for asset fixing id {assetFixingId}");

                    model.TryGetFixingDictionary(fxFixingId, out var fxFixingDict);
                    if (fxFixingDict == null)
                        throw new Exception($"Fx Fixing dictionary not found for asset fixing id {fxFixingId}");

                    var assetFixings = new List<double>();
                    var fxFixingsA = new List<double>();
                    foreach (var d in alreadyFixed)
                    {
                        if (!fixingDict.TryGetValue(d, out var f))
                            throw new Exception($"Fixing for date {d:yyyy-MM-dd} not found in dictionary for asset fixing id {assetFixingId}");
                        assetFixings.Add(f);

                        if (!fxFixingDict.TryGetValue(d, out var ffx))
                            throw new Exception($"Fixing for date {d:yyyy-MM-dd} not found in dictionary for fx fixing id {fxFixingId}");
                        fxFixingsA.Add(ffx);

                    }

                    fixedAvg = assetFixings.Select((x, ix) => x * fxFixingsA[ix]).Average();
                }

                var floatAsset = stillToFix.AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag)
                    .Select(d => priceCurve.GetPriceForDate(d));
                var floatFx = model.FundingModel.GetFxRates(stillToFix, priceCurve.Currency, swap.PaymentCurrency).ToArray();
                var floatCompoAvg = floatAsset.Any() ? floatAsset.Select((x, ix) => x * floatFx[ix]).Average() : 0.0;

                return (FixedAverage: fixedAvg, FloatAverage: floatCompoAvg, FixedCount: alreadyFixed.Count(), FloatCount: stillToFix.Count());
            }
        }

        public static double PV(this AsianSwapStrip asianSwap, IAssetFxModel model) => asianSwap.Swaplets.Sum(x => x.PV(model));

        public static double PV(this AsianBasisSwap asianBasisSwap, IAssetFxModel model)
        {
            var payPV = asianBasisSwap.PaySwaplets.Sum(x => x.PV(model));
            var recPV = asianBasisSwap.RecSwaplets.Sum(x => x.PV(model));
            return payPV + recPV;
        }

        public static double PV(this Future future, IAssetFxModel model)
        {
            if (future.ExpiryDate < model.BuildDate)
                return 0.0;

            var price = model.GetPriceCurve(future.AssetId).GetPriceForDate(future.ExpiryDate);
            return (price - future.Strike) * future.ContractQuantity * future.LotSize * future.PriceMultiplier;
        }

        public static double PV(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().PV(model);

        public static double FlowsT0(this AsianOption asianOption, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            if (payDate != model.BuildDate)
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            return asianOption.Notional * (asianOption.CallPut == OptionType.Call ? System.Math.Max(0, FixedAverage - asianOption.Strike) : System.Math.Max(0, asianOption.Strike - FixedAverage));
        }

        public static double FlowsT0(this AsianSwap asianSwap, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianSwap.AssetId);

            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
             asianSwap.AverageEndDate.AddPeriod(asianSwap.PaymentLagRollType, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
             asianSwap.PaymentDate;

            if (payDate != model.BuildDate)
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianSwap.GetAveragesForSwap(model);
            return asianSwap.Notional * (FixedAverage - asianSwap.Strike);
        }

        public static double FlowsT0(this Future future, IAssetFxModel model)
        {
            if (future.ExpiryDate != model.BuildDate)
                return 0.0;

            var price = model.GetPriceCurve(future.AssetId).GetPriceForDate(future.ExpiryDate);
            return (price - future.Strike) * future.ContractQuantity * future.LotSize * future.PriceMultiplier;
        }

        public static double FlowsT0(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().FlowsT0(model);

        public static double FlowsT0(this AsianSwapStrip asianSwap, IAssetFxModel model) => asianSwap.Swaplets.Sum(x => x.FlowsT0(model));

        public static double FlowsT0(this AsianBasisSwap asianBasisSwap, IAssetFxModel model)
        {
            var payPV = asianBasisSwap.PaySwaplets.Sum(x => x.FlowsT0(model));
            var recPV = asianBasisSwap.RecSwaplets.Sum(x => x.FlowsT0(model));
            return payPV + recPV;
        }

        public static ICube PV(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency=null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) }
            };
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
                    case AsianBasisSwap basisSwap:
                        pv = basisSwap.PV(model);
                        tradeId = basisSwap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, basisSwap.PaySwaplets.First().PaymentCurrency);
                        else
                            ccy = basisSwap.PaySwaplets.First().PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        pv = fwd.PV(model);
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    case Future fut:
                        pv = fut.PV(model);
                        tradeId = fut.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fut.Currency);
                        else
                            ccy = fut.Currency.ToString();
                        break;
                    case FxForward fxFwd:
                        pv = fxFwd.Pv(model.FundingModel, false);
                        tradeId = fxFwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        pv = loanDepo.Pv(model.FundingModel, false);
                        tradeId = loanDepo.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }


                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy }
                };
                cube.AddRow(row,pv/fxRate);
            }

            return cube;
        }

        public static ICube FlowsT0(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) }
            };
            cube.Initialize(dataTypes);

            foreach (var ins in portfolio.Instruments)
            {
                var pv = 0.0;
                var fxRate = 1.0;
                string tradeId = null;
                var ccy = reportingCurrency?.ToString();
                switch (ins)
                {
                    case AsianOption asianOption:
                        pv = asianOption.FlowsT0(model);
                        tradeId = asianOption.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, asianOption.PaymentCurrency);
                        else
                            ccy = asianOption.PaymentCurrency.ToString();
                        break;
                    case AsianSwap swap:
                        pv = swap.FlowsT0(model);
                        tradeId = swap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swap.PaymentCurrency);
                        else
                            ccy = swap.PaymentCurrency.ToString();
                        break;
                    case AsianSwapStrip swapStrip:
                        pv = swapStrip.FlowsT0(model);
                        tradeId = swapStrip.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swapStrip.Swaplets.First().PaymentCurrency);
                        else
                            ccy = swapStrip.Swaplets.First().PaymentCurrency.ToString();
                        break;
                    case AsianBasisSwap basisSwap:
                        pv = basisSwap.FlowsT0(model);
                        tradeId = basisSwap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, basisSwap.PaySwaplets.First().PaymentCurrency);
                        else
                            ccy = basisSwap.PaySwaplets.First().PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        pv = fwd.FlowsT0(model);
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    case Future fut:
                        pv = fut.FlowsT0(model);
                        tradeId = fut.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fut.Currency);
                        else
                            ccy = fut.Currency.ToString();
                        break;
                    case FxForward fxFwd:
                        pv = fxFwd.FlowsT0(model.FundingModel);
                        tradeId = fxFwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        pv = loanDepo.FlowsT0(model.FundingModel);
                        tradeId = loanDepo.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }


                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy }
                };
                cube.AddRow(row, pv / fxRate);
            }

            return cube;
        }

        public static ICube AssetDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments.Where(x => (x is IAssetInstrument ia) && ia.AssetIds.Contains(curveObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var pvCube = subPortfolio.PV(model, curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex("TradeId");

                var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);

                foreach (var bCurve in bumpedCurves)
                {
                    var newModel = model.Clone();
                    newModel.AddPriceCurve(curveName, bCurve.Value);
                    var bumpedPVCube = subPortfolio.PV(newModel, curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize;

                        if (delta != 0.0)
                        {
                            if (bCurve.Value.UnderlyingsAreForwards) //de-discount delta
                                delta /= GetUsdDF(model, (PriceCurve)bCurve.Value, bCurve.Value.PillarDatesForLabel(bCurve.Key));

                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "AssetId", curveName },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "Delta" }
                            };
                            cube.AddRow(row, delta);
                        }
                    }
                }
            }
            return cube;
        }

        private static double GetUsdDF(IAssetFxModel model, PriceCurve priceCurve, DateTime fwdDate)
        {
            var colSpec = priceCurve.CollateralSpec;
            var ccy = priceCurve.Currency;
            var disccurve = model.FundingModel.GetCurveByCCyAndSpec(ccy, colSpec);
            return disccurve.GetDf(model.BuildDate, fwdDate);
        }

        public static ICube FxDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var domCcy = model.FundingModel.FxMatrix.BaseCurrency;

            foreach (var currency in model.FundingModel.FxMatrix.SpotRates.Keys)
            {
                var pvCube = portfolio.PV(model, currency);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");

                var fxPair = $"{domCcy}/{currency}";
                var spot = model.FundingModel.FxMatrix.SpotRates[currency];
                var bumpedSpot = spot + bumpSize;
                var newModel = model.Clone();
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var bumpedPVCube = portfolio.PV(newModel, currency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");
                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize;

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                        {
                            { "TradeId", bumpedRows[i].MetaData[tidIx] },
                            { "AssetId", fxPair },
                            { "Metric", "FxSpotDelta" }
                        };
                        cube.AddRow(row, delta);
                    }
                }

            }
            return cube;
        }

        public static ICube AssetDeltaGamma(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.1;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);



            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments.Where(x => (x is IAssetInstrument ia) && ia.AssetIds.Contains(curveObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var pvCube = subPortfolio.PV(model, curveObj.Currency);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var bumpedUpCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);
                var bumpedDownCurves = curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook);
                foreach (var bUpCurve in bumpedUpCurves)
                {
                    var newModelUp = model.Clone();
                    var newModelDown = model.Clone();
                    newModelUp.AddPriceCurve(curveName, bUpCurve.Value);
                    newModelDown.AddPriceCurve(curveName, bumpedDownCurves[bUpCurve.Key]);
                    var bumpedUpPVCube = subPortfolio.PV(newModelUp, curveObj.Currency);
                    var bumpedDownPVCube = subPortfolio.PV(newModelDown, curveObj.Currency);
                    var bumpedUpRows = bumpedUpPVCube.GetAllRows();
                    var bumpedDownRows = bumpedDownPVCube.GetAllRows();

                    if (bumpedUpRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");

                    for (var i = 0; i < bumpedUpRows.Length; i++)
                    {
                        var deltaUp = ((double)bumpedUpRows[i].Value - (double)pvRows[i].Value) / bumpSize;
                        var deltaDown = ((double)pvRows[i].Value - (double)bumpedDownRows[i].Value ) / bumpSize;

                        if (deltaUp != 0.0 || deltaDown != 0.0)
                        {
                            if (bUpCurve.Value.UnderlyingsAreForwards) //de-discount delta
                            {
                                var df = model.FundingModel.Curves["USD.LIBOR.3M"].GetDf(model.BuildDate, bUpCurve.Value.PillarDatesForLabel(bUpCurve.Key));
                                deltaUp /= df;
                                deltaDown /= df;
                            }

                            var gamma = (deltaUp - deltaDown) / bumpSize;
                            var delta = 0.5 * (deltaUp + deltaDown);

                            if (delta != 0.0 || gamma != 0.0)
                            {
                                var row = new Dictionary<string, object>
                                {
                                    { "TradeId", bumpedUpRows[i].MetaData[tidIx] },
                                    { "AssetId", curveName },
                                    { "PointLabel", bUpCurve.Key },
                                    { "Metric", "Delta" }
                                };
                                cube.AddRow(row, delta);

                                var rowG = new Dictionary<string, object>
                                {
                                    { "TradeId", bumpedUpRows[i].MetaData[tidIx] },
                                    { "AssetId", curveName },
                                    { "PointLabel", bUpCurve.Key },
                                    { "Metric", "Gamma" }
                                };
                                cube.AddRow(rowG, gamma);
                            }
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube AssetVega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);


            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var volObj = model.GetVolSurface(surfaceName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments.Where(x => (x is IHasVega) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var pvCube = subPortfolio.PV(model, reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");

                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                foreach (var bCurve in bumpedSurfaces)
                {
                    var newModel = model.Clone();
                    newModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPVCube = subPortfolio.PV(newModel, reportingCcy);
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

        public static ICube FxVega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);



            foreach (var surfaceName in model.FundingModel.VolSurfaces.Keys)
            {
                var volObj = model.FundingModel.VolSurfaces[surfaceName];

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments.Where(x => (x is IHasVega)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var pvCube = subPortfolio.PV(model, reportingCcy);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);
                foreach (var bCurve in bumpedSurfaces)
                {
                    var newModel = model.Clone();
                    newModel.FundingModel.VolSurfaces[surfaceName] = bCurve.Value;
                    var bumpedPVCube = subPortfolio.PV(newModel, reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "AssetId", surfaceName },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "Vega" }
                            };
                            cube.AddRow(row, vega);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube AssetIrDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var bumpSize = 0.0001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "CurveName", typeof(string) },
                { "PointLabel", typeof(DateTime) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            foreach (var curve in model.FundingModel.Curves)
            {
                var curveObj = curve.Value;

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) && ia.IrCurves.Contains(curve.Key))
                    .Concat(portfolio.Instruments.Where(x=>(x is FxForward fx) && (model.FundingModel.FxMatrix.DiscountCurveMap[fx.DomesticCCY]==curve.Key || model.FundingModel.FxMatrix.DiscountCurveMap[fx.ForeignCCY] == curve.Key || fx.ForeignDiscountCurve == curve.Key)))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var pvCube = subPortfolio.PV(model, curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex("TradeId");

                var bumpedCurves = curveObj.BumpScenarios(bumpSize, lastDateInBook);

                foreach (var bCurve in bumpedCurves)
                {
                    var newModel = model.Clone();
                    newModel.FundingModel.Curves[curve.Key] = bCurve.Value;
                    var bumpedPVCube = subPortfolio.PV(newModel, curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value);

                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "CurveName", curve.Key },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "IrDelta" }
                            };
                            cube.AddRow(row, delta);
                        }
                    }
                }
            }
            return cube;
        }

        public static ICube CorrelationDelta(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy, double epsilon)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model, reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex("TradeId");

            var bumpedCorrelMatrix = model.CorrelationMatrix.Bump(epsilon);

            var newModel = model.Clone();
            newModel.CorrelationMatrix = bumpedCorrelMatrix;
            var bumpedPVCube = portfolio.PV(newModel, reportingCcy);
            var bumpedRows = bumpedPVCube.GetAllRows();
            if (bumpedRows.Length != pvRows.Length)
                throw new Exception("Dimensions do not match");
            for (var i = 0; i < bumpedRows.Length; i++)
            {
                //flat bump of correlation matrix by single epsilon parameter, reported as PnL
                var cDelta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value); 
                if (cDelta != 0.0)
                {
                    var row = new Dictionary<string, object>();
                    row.Add("TradeId", bumpedRows[i].MetaData[tidIx]);
                    row.Add("Metric", "CorrelDelta");
                    cube.AddRow(row, cDelta);
                }
            }

            return cube;
        }

        public static ICube AssetTheta(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model, reportingCcy);
            var cashCube = portfolio.FlowsT0(model, reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var cashRows = cashCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex("TradeId");

            var rolledModel = model.RollModel(fwdValDate);

            var pvCubeFwd = portfolio.PV(rolledModel, reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            for (var i = 0; i < pvRowsFwd.Length; i++)
            {
                var theta = pvRowsFwd[i].Value - pvRows[i].Value;
                if (theta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", pvRowsFwd[i].MetaData[tidIx] },
                        { "Metric", "Theta" }
                    };
                    cube.AddRow(row, theta);
                }
            }

            for (var i = 0; i < cashRows.Length; i++)
            {
                var cash = cashRows[i].Value;
                if (cash != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", cashRows[i].MetaData[tidIx] },
                        { "Metric", "CashMove" }
                    };
                    cube.AddRow(row, cash);
                }
            }

            return cube;
        }

        public static ICube AssetThetaCharm(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model, reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = portfolio.FlowsT0(model, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex("TradeId");

            var rolledModel = model.RollModel(fwdValDate);

            var pvCubeFwd = portfolio.PV(rolledModel, reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            //theta
            for (var i = 0; i < pvRowsFwd.Length; i++)
            {
                var theta = pvRowsFwd[i].Value - pvRows[i].Value;
                if (theta != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", pvRowsFwd[i].MetaData[tidIx] },
                        { "AssetId", string.Empty },
                        { "PointLabel", string.Empty },
                        { "Metric", "Theta" }
                    };
                    cube.AddRow(row, theta);
                }
            }

            //cash move
            for (var i = 0; i < cashRows.Length; i++)
            {
                var cash = cashRows[i].Value;
                if (cash != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", cashRows[i].MetaData[tidIx] },
                        { "AssetId", string.Empty },
                        { "PointLabel", "CashMove" },
                        { "Metric", "Theta" }
                    };
                    cube.AddRow(row, cash);
                }
            }

            //charm-asset
            var baseDeltaCube = AssetDelta(portfolio, model);
            var rolledDeltaCube = AssetDelta(portfolio, rolledModel);
            var charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var plId = charmCube.GetColumnIndex("PointLabel");
            var aId = charmCube.GetColumnIndex("AssetId");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "AssetId", charmRow.MetaData[aId] },
                    { "PointLabel", charmRow.MetaData[plId] },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            //charm-fx
            baseDeltaCube = FxDelta(portfolio, model);
            rolledDeltaCube = FxDelta(portfolio, rolledModel);
            charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var fId = charmCube.GetColumnIndex("AssetId");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "AssetId", charmRow.MetaData[fId] },
                    { "PointLabel", string.Empty },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            return cube;
        }

        public static ICube AssetGreeks(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy)
        {
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) },
                { "Currency", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model, reportingCcy);
            var pvRows = pvCube.GetAllRows();

            var cashCube = portfolio.FlowsT0(model, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var tidIx = pvCube.GetColumnIndex("TradeId");

            var rolledModel = model.RollModel(fwdValDate);

            var pvCubeFwd = portfolio.PV(rolledModel, reportingCcy);
            var pvRowsFwd = pvCubeFwd.GetAllRows();

            //theta
            var thetaCube = pvCubeFwd.QuickDifference(pvCube);
            cube = cube.Merge(thetaCube, new Dictionary<string, object>
            {
                 { "AssetId", string.Empty },
                 { "PointLabel", string.Empty },
                 { "Metric", "Theta" }
            });


            //cash move
            cube = cube.Merge(cashCube, new Dictionary<string, object>
            {
                 { "AssetId", string.Empty },
                 { "PointLabel", "CashMove"  },
                 { "Metric", "Theta" }
            });

            //delta
            var baseDeltaCube = AssetDelta(portfolio, model);
            cube = cube.Merge(baseDeltaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //vega
            var assetVegaCube = AssetVega(portfolio, model, reportingCcy);
            cube = cube.Merge(assetVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });
            var fxVegaCube = FxVega(portfolio, model, reportingCcy);
            cube = cube.Merge(fxVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //charm-asset
            var rolledDeltaCube = AssetDelta(portfolio, rolledModel);
            var charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            cube = cube.Merge(charmCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
                 { "Metric", "Charm"},
            });
            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { "Metric", "AssetDeltaT1" },
            });

            //charm-fx
            baseDeltaCube = FxDelta(portfolio, model);
            cube = cube.Merge(baseDeltaCube, new Dictionary<string, object>
            {
                 { "PointLabel", string.Empty },
                 { "Currency", string.Empty },
            });
            rolledDeltaCube = FxDelta(portfolio, rolledModel);
            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { "PointLabel", string.Empty },
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { "Metric", "FxSpotDeltaT1" },
            });
            charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var fId = charmCube.GetColumnIndex("AssetId");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "AssetId", charmRow.MetaData[fId] },
                    { "PointLabel", string.Empty },
                    { "Currency", string.Empty },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            //ir-delta
            var irBumpSize = 0.0001;
            foreach (var curve in model.FundingModel.Curves)
            {
                var curveObj = curve.Value;

                var lastDateInBook = portfolio.LastSensitivityDate();
                var bumpedCurves = curveObj.BumpScenarios(irBumpSize, lastDateInBook);

                foreach (var bCurve in bumpedCurves)
                {
                    var newModel = model.Clone();
                    newModel.FundingModel.Curves[curve.Key] = bCurve.Value;
                    var bumpedPVCube = portfolio.PV(newModel, reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = ((double)bumpedRows[i].Value - (double)pvRows[i].Value);

                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "AssetId", curve.Key },
                                { "PointLabel", bCurve.Key },
                                { "Currency", reportingCcy.Ccy},
                                { "Metric", "IrDelta" }
                            };
                            cube.AddRow(row, delta);
                        }
                    }
                }
            }

            return cube;
        }

        public static IAssetFxModel RollModel(this IAssetFxModel model, DateTime fwdValDate)
        {
            //setup the "tomorrow" scenario
            var rolledIrCurves = new Dictionary<string, IrCurve>();
            foreach (var curve in model.FundingModel.Curves)
            {
                var rolledCurve = curve.Value.RebaseDate(fwdValDate);
                rolledIrCurves.Add(curve.Key, rolledCurve);
            }

            var rolledFxVolSurfaces = new Dictionary<string, IVolSurface>();
            foreach (var surface in model.FundingModel.VolSurfaces)
            {
                rolledFxVolSurfaces.Add(surface.Key, surface.Value);
            }

            var matrix = model.FundingModel.FxMatrix;
            var pairs = matrix.FxPairDefinitions;
            var ccys = matrix.SpotRates.Keys;
            var pairsByCcy = ccys.ToDictionary(c => c, c => matrix.GetFxPair(matrix.BaseCurrency, c));
            var newSpotDates = pairsByCcy.ToDictionary(p => p.Key, p => p.Value.SpotDate(fwdValDate));
            var newSpotRates = newSpotDates.ToDictionary(kv => kv.Key, kv => model.FundingModel.GetFxRate(kv.Value, matrix.BaseCurrency, kv.Key));
            var rolledFxMatrix = model.FundingModel.FxMatrix.Rebase(fwdValDate, newSpotRates);


            var rolledFundingModel = model.FundingModel.DeepClone(fwdValDate);
            rolledFundingModel.UpdateCurves(rolledIrCurves);
            rolledFundingModel.VolSurfaces = rolledFxVolSurfaces;
            rolledFundingModel.SetupFx(rolledFxMatrix);


            var rolledPriceCurves = new Dictionary<string, IPriceCurve>();
            foreach (var curveName in model.CurveNames)
            {
                var rolledCurve = model.GetPriceCurve(curveName).RebaseDate(fwdValDate);
                rolledPriceCurves.Add(curveName, rolledCurve);
            }

            var rolledVolSurfaces = new Dictionary<string, IVolSurface>();
            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var rolledSurface = model.GetVolSurface(surfaceName);
                rolledVolSurfaces.Add(surfaceName, rolledSurface);
            }

            var rolledFixings = new Dictionary<string, IFixingDictionary>();
            foreach (var fixingName in model.FixingDictionaryNames)
            {
                var rolledDictionary = model.GetFixingDictionary(fixingName);
                var newDict = rolledDictionary.Clone();
                var date = model.BuildDate;
                while (date < fwdValDate)
                {
                    if (!newDict.ContainsKey(date))
                    {
                        if (newDict.FixingDictionaryType == FixingDictionaryType.Asset)
                        {
                            var curve = (PriceCurve)model.GetPriceCurve(newDict.AssetId);
                            var estFixing = curve.GetPriceForDate(date.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag));
                            newDict.Add(date, estFixing);
                        }
                        else //its FX
                        {
                            var id = newDict.AssetId;
                            var ccyLeft = new Currency(id.Substring(0, 3), DayCountBasis.Act365F, null);
                            var ccyRight = new Currency(id.Substring(id.Length - 3, 3), DayCountBasis.Act365F, null);
                            var pair = model.FundingModel.FxMatrix.GetFxPair(ccyLeft, ccyRight);
                            var spotDate = pair.SpotDate(date);
                            var estFixing = model.FundingModel.GetFxRate(spotDate, ccyLeft, ccyRight);
                            newDict.Add(date, estFixing);
                        }
                    }

                    date = date.AddDays(1);
                }
                rolledFixings.Add(fixingName, newDict);

            }

            var rolledModel = new AssetFxModel(fwdValDate, rolledFundingModel);
            rolledModel.AddPriceCurves(rolledPriceCurves);
            rolledModel.AddVolSurfaces(rolledVolSurfaces);
            rolledModel.AddFixingDictionaries(rolledFixings);
            rolledModel.CorrelationMatrix = model.CorrelationMatrix;

            return rolledModel;
        }

        public static string[] GetRequiredPriceCurves(this Portfolio portfolio)
        {
            var curves = portfolio.Instruments
                .Where(p => p is IAssetInstrument)
                .SelectMany(a => ((IAssetInstrument)a).AssetIds)
                .Distinct();
            return curves.ToArray();
        }

        public static List<MarketDataDescriptor> GetRequirements(this Portfolio portfolio, DateTime valDate)
        {
            var o = new List<MarketDataDescriptor>();

            foreach (var trade in portfolio.Instruments)
            {
                switch (trade)
                {
                    case AsianOption aOpt:
                        {
                            o.Add(new AssetCurveDescriptor { AssetId = aOpt.AssetId, ValDate = valDate });
                            o.Add(new AssetVolSurfaceDescriptor { AssetId = aOpt.AssetId, ValDate = valDate });
                            foreach (var fixingDate in aOpt.FixingDates.Where(f => f < valDate))
                            {
                                o.Add(new AssetFixingDescriptor { AssetId = aOpt.AssetId, FixingDate = fixingDate });
                            }
                            if (aOpt.FxConversionType != FxConversionType.None)
                            {
                                var fxId = aOpt.GetFxFixingId("USD");
                                var fxDates = aOpt.FxFixingDates ?? aOpt.FixingDates;
                                foreach (var fixingDate in fxDates.Where(f => f < valDate))
                                {
                                    o.Add(new AssetFixingDescriptor { AssetId = fxId, FixingDate = fixingDate });
                                }
                            }

                            break;
                        }
                    case AsianSwap aSwp:
                        {
                            o.Add(new AssetCurveDescriptor { AssetId = aSwp.AssetId, ValDate = valDate });
                            foreach (var fixingDate in aSwp.FixingDates.Where(f => f < valDate))
                            {
                                o.Add(new AssetFixingDescriptor { AssetId = aSwp.AssetId, FixingDate = fixingDate });
                            }
                            if (aSwp.FxConversionType != FxConversionType.None)
                            {
                                var fxId = aSwp.GetFxFixingId("USD");
                                var fxDates = aSwp.FxFixingDates ?? aSwp.FixingDates;
                                foreach (var fixingDate in fxDates.Where(f => f < valDate))
                                {
                                    o.Add(new AssetFixingDescriptor { AssetId = fxId, FixingDate = fixingDate });
                                }
                            }

                            break;
                        }
                    case Future aFut:
                        {
                            o.Add(new AssetCurveDescriptor { AssetId = aFut.AssetId, ValDate = valDate });
                            break;
                        }
                }
            }

            return o;
        }

        public static double ParRate(this IAssetInstrument instrument, IAssetFxModel model)
        {
            var objectiveFunc = new Func<double, double>(k =>
            {
                var ik = instrument.SetStrike(k);
                var p = new Portfolio { Instruments = new List<IInstrument> { ik } };
                return p.PV(model).GetAllRows().First().Value;
            });

            var fairK = Math.Solvers.Brent.BrentsMethodSolve(objectiveFunc, -1e6, 1e6, 1e-10);
            return fairK;
        }
    }
}
