using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Risk;
using Qwack.Options;
using Qwack.Options.Asians;
using Qwack.Options.VolSurfaces;
using Qwack.Utils.Parallel;
using static System.Math;

namespace Qwack.Models.Models
{
    public static class AssetProductEx
    {
        private static readonly bool _useFuturesMethod = true;

        public static double PV(this AsianOption asianOption, IAssetFxModel model, bool ignoreTodayFlows)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            if (payDate < model.BuildDate || (ignoreTodayFlows && payDate == model.BuildDate))
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            var fwd = FloatAverage;
            var fixedAvg = FixedAverage;

            if (asianOption.FixingDates.Last() <= model.BuildDate)
            {
                var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FixedCount + FloatCount);
                return asianOption.Notional * (asianOption.CallPut == OptionType.Call ? Max(0, avg - asianOption.Strike) : Max(0, asianOption.Strike - avg));
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

                if (adjustedStrike < 0) //its delta-1
                {
                    var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FixedCount + FloatCount);
                    return asianOption.CallPut == OptionType.Put ? 0.0 : (avg - asianOption.Strike) * asianOption.Notional;
                }
            }

            var discountCurve = model.FundingModel.GetCurve(asianOption.DiscountCurve);
            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            if (_useFuturesMethod)
            {
                (var fwds, var todayFixed) = asianOption.GetFwdVector(model);
                var moneyness = adjustedStrike / FloatAverage;
                var sigmas = asianOption.FixingDates.Select((f, ix) => surface.GetVolForAbsoluteStrike(fwds[ix] * moneyness, f, fwds[ix])).ToArray();

                if (isCompo)
                {
                    var fxId = $"{curve.Currency.Ccy}/{asianOption.PaymentCurrency.Ccy}";
                    var fxPair = model.FundingModel.FxMatrix.GetFxPair(fxId);

                    for (var i = 0; i < sigmas.Length; i++)
                    {
                        var fxSpotDate = fxPair.SpotDate(asianOption.FixingDates[i]);
                        var fxFwd = model.FundingModel.GetFxRate(fxSpotDate, fxId);
                        var fxVol = model.FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, asianOption.FixingDates[i], fxFwd);
                        var tExpC = model.BuildDate.CalculateYearFraction(asianOption.FixingDates[i], DayCountBasis.Act365F);
                        var correl = model.CorrelationMatrix.GetCorrelation(fxId, asianOption.AssetId, tExpC);
                        sigmas[i] = Sqrt(sigmas[i] * sigmas[i] + fxVol * fxVol + 2 * correl * fxVol * sigmas[i]);
                    }
                }

                return TurnbullWakeman.PV(fwds, asianOption.FixingDates, model.BuildDate, asianOption.PaymentDate, sigmas, asianOption.Strike, riskFree, asianOption.CallPut, todayFixed) * asianOption.Notional;
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
                var fxVol = model.FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, volDate, fxVolFwd);
                var correl = model.CorrelationMatrix.GetCorrelation(fxId, asianOption.AssetId);
                sigma = Sqrt(sigma * sigma + fxVol * fxVol + 2 * correl * fxVol * sigma);
            }



            return TurnbullWakeman.PV(fwd, fixedAvg, sigma, asianOption.Strike, model.BuildDate, asianOption.AverageStartDate, asianOption.AverageEndDate, riskFree, asianOption.CallPut) * asianOption.Notional;
        }

        public static double PV(this AsianSwap asianSwap, IAssetFxModel model, bool ignoreTodayFlows)
        {
            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
                asianSwap.AverageEndDate.AddPeriod(asianSwap.PaymentLagRollType, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
                asianSwap.PaymentDate;

            if (payDate < model.BuildDate || (ignoreTodayFlows && payDate == model.BuildDate))
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianSwap.GetAveragesForSwap(model);
            var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FloatCount + FixedCount);
            var pv = avg - asianSwap.Strike;
            pv *= asianSwap.Direction == TradeDirection.Long ? 1.0 : -1.0;
            pv *= asianSwap.Notional;
            pv *= model.FundingModel.GetDf(asianSwap.DiscountCurve, model.BuildDate, payDate);

            return pv;
        }

        public static string GetFxFixingId(this AsianSwap swap, string curveCcy) => string.IsNullOrEmpty(swap.FxFixingId) ? $"{curveCcy}/{swap.PaymentCurrency.Ccy}" : swap.FxFixingId;

        public static (double FixedAverage, double FloatAverage, int FixedCount, int FloatCount) GetAveragesForSwap(this AsianSwap swap, IAssetFxModel model)
        {
            var fxDates = swap.FxFixingDates ?? swap.FixingDates;
            var sampleDates = swap.FixingDates.AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag);
            var priceCurve = model.GetPriceCurve(swap.AssetId);
            var fxFixingId = swap.GetFxFixingId(priceCurve.Currency.Ccy);
            var assetFixingId = swap.AssetFixingId ?? swap.AssetId;

            if (swap.PaymentCurrency == priceCurve.Currency || swap.FxConversionType == FxConversionType.AverageThenConvert)
            {
                var fxAverage = 1.0;

                if (model.BuildDate < swap.FixingDates.First())
                {
                    var fwds = sampleDates.Select(x => priceCurve.GetPriceForDate(x)).ToArray();
                    var avg = fwds.Average();

                    if (swap.FxConversionType == FxConversionType.AverageThenConvert)
                    {
                        fxAverage = model.FundingModel.GetFxAverage(fxDates, priceCurve.Currency, swap.PaymentCurrency);
                        avg *= fxAverage;
                        for (var i = 0; i < fwds.Length; i++)
                            fwds[i] *= fxAverage;
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

                        fixedAvg = alreadyFixed.Select(d => fixingDict.GetFixing(d)).Average();
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

                            fixedFxAvg = alreadyFixedFx.Select(d => fxFixingDict.GetFixing(d)).Average();
                        }
                        var floatFxAvg = stillToFixFx.Any() ? model.FundingModel.GetFxAverage(stillToFixFx, priceCurve.Currency, swap.PaymentCurrency) : 0.0;
                        var fxAvg = (fixedFxAvg * alreadyFixedFx.Count() + floatFxAvg * stillToFixFx.Count()) / (alreadyFixedFx.Count() + stillToFixFx.Count());
                        floatAvg *= fxAvg;
                        fixedAvg *= fxAvg;
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
                        assetFixings.Add(fixingDict.GetFixing(d));
                        fxFixingsA.Add(fxFixingDict.GetFixing(d));
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

        public static (double[] fwds, bool todayIsFixed) GetFwdVector(this AsianSwap swap, IAssetFxModel model)
        {
            var fxDates = swap.FxFixingDates ?? swap.FixingDates;
            var priceCurve = model.GetPriceCurve(swap.AssetId);
            var fxFixingId = swap.GetFxFixingId(priceCurve.Currency.Ccy);
            var assetFixingId = swap.AssetFixingId ?? swap.AssetId;

            var fwds = new double[swap.FixingDates.Length];

            //asset fwds & fixings first
            var fixingForToday = swap.AverageStartDate <= model.BuildDate &&
                            model.TryGetFixingDictionary(assetFixingId, out var fixings) &&
                            fixings.TryGetValue(model.BuildDate, out var todayFixing);

            IFixingDictionary fixingDict = null;
            if (fixingForToday || swap.FixingDates.First() < model.BuildDate)
            {
                model.TryGetFixingDictionary(assetFixingId, out fixingDict);
                if (fixingDict == null)
                    throw new Exception($"Fixing dictionary not found for asset fixing id {assetFixingId}");
            }

            for (var i = 0; i < swap.FixingDates.Length; i++)
            {
                if (swap.FixingDates[i] < model.BuildDate || (fixingForToday && swap.FixingDates[i] == model.BuildDate))
                    fwds[i] = fixingDict.GetFixing(swap.FixingDates[i]);
                else
                    fwds[i] = priceCurve.GetPriceForDate(swap.FixingDates[i].AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag));
            }


            if (swap.PaymentCurrency == priceCurve.Currency)
                return (fwds, fixingForToday);
            else
            {
                var fixingForTodayFx = fxDates.First() <= model.BuildDate &&
                            model.TryGetFixingDictionary(fxFixingId, out var fxFixings) &&
                            fxFixings.TryGetValue(model.BuildDate, out var todayFxFixing);
                var fxPair = model.FundingModel.FxMatrix.GetFxPair(priceCurve.Currency, swap.PaymentCurrency);
                var fxRates = new double[fxDates.Length];
                IFixingDictionary fxFixingDict = null;
                if (fixingForTodayFx || fxDates.First() < model.BuildDate)
                {
                    model.TryGetFixingDictionary(fxFixingId, out fxFixingDict);
                    if (fxFixingDict == null)
                        throw new Exception($"Fx Fixing dictionary not found for asset fixing id {fxFixingId}");
                }


                for (var i = 0; i < fxDates.Length; i++)
                {
                    if (swap.FixingDates[i] < model.BuildDate || (fixingForTodayFx && swap.FixingDates[i] == model.BuildDate))
                        fxRates[i] = fxFixingDict.GetFixing(fxDates[i]);
                    else

                        fxRates[i] = model.FundingModel.GetFxRate(fxPair.SpotDate(fxDates[i]), priceCurve.Currency, swap.PaymentCurrency);
                }

                if (swap.FxConversionType == FxConversionType.AverageThenConvert)
                {
                    var fxAvg = fxRates.Average();
                    return (fwds.Select(f => f * fxAvg).ToArray(), fixingForToday);
                }
                else
                {
                    return (fwds.Select((f, ix) => f * fxRates[ix]).ToArray(), fixingForToday); 
                }
            }
        }


        public static double PV(this AsianSwapStrip asianSwap, IAssetFxModel model, bool ignoreTodayFlows) => asianSwap.Swaplets.Sum(x => x.PV(model, ignoreTodayFlows));

        public static double PV(this AsianBasisSwap asianBasisSwap, IAssetFxModel model, bool ignoreTodayFlows)
        {
            var payPV = asianBasisSwap.PaySwaplets.Sum(x => x.PV(model, ignoreTodayFlows));
            var recPV = asianBasisSwap.RecSwaplets.Sum(x => x.PV(model, ignoreTodayFlows));
            return payPV + recPV;
        }

        public static double PV(this Future future, IAssetFxModel model, bool ignoreTodayFlows)
        {

            if (future.ExpiryDate < model.BuildDate || (ignoreTodayFlows && future.ExpiryDate == model.BuildDate))
                return 0.0;
            var curve = model.GetPriceCurve(future.AssetId);

            var price = 0.0;
            if (curve.CurveType == PriceCurveType.NextButOnExpiry)
                price = curve.GetPriceForDate(future.ExpiryDate.AddDays(-1));
            else
                price = curve.GetPriceForDate(future.ExpiryDate);

            return (price - future.Strike) * future.ContractQuantity * future.LotSize * future.PriceMultiplier;
        }

        public static double PV(this FuturesOption option, IAssetFxModel model, bool ignoreTodayFlows)
        {
            if (option.ExpiryDate < model.BuildDate || (ignoreTodayFlows && option.ExpiryDate == model.BuildDate))
                return 0.0;

            if (!(option.ExerciseType == OptionExerciseType.European ||
                (option.ExerciseType == OptionExerciseType.American && option.MarginingType == OptionMarginingType.FuturesStyle)
                ))
                throw new Exception("Only European style options currently supported");

            var price = model.GetPriceCurve(option.AssetId).GetPriceForDate(option.ExpiryDate);

            var df = option.MarginingType == OptionMarginingType.FuturesStyle ? 1.0
                : model.FundingModel.GetDf(option.DiscountCurve, model.BuildDate, option.ExpiryDate);
            var t = model.BuildDate.CalculateYearFraction(option.ExpiryDate.AddHours(18), DayCountBasis.Act365F, false);
            var vol = model.GetVolForStrikeAndDate(option.AssetId, option.ExpiryDate, option.Strike);

            var fv = BlackFunctions.BlackPV(price, option.Strike, 0.0, t, vol, option.CallPut);
            if (option.Premium != 0)
            {
                fv -= option.Premium;
            }

            return fv * df * option.ContractQuantity * option.LotSize;
        }

        public static double PV(this Forward fwd, IAssetFxModel model, bool ignoreTodayFlows) => fwd.AsBulletSwap().PV(model, ignoreTodayFlows);

        public static double PV(this EuropeanOption euOpt, IAssetFxModel model, bool ignoreTodayFlows)
        {
            if (euOpt.PaymentDate < model.BuildDate || (ignoreTodayFlows && euOpt.PaymentDate == model.BuildDate))
                return 0.0;

            var isCompo = euOpt.Currency != model.GetPriceCurve(euOpt.AssetId).Currency;
            var curve = model.GetPriceCurve(euOpt.AssetId);
            var fwdDate = euOpt.ExpiryDate.AddPeriod(RollType.F, euOpt.FixingCalendar, euOpt.SpotLag);
            var fwd = curve.GetPriceForDate(fwdDate);
            var fxFwd = isCompo ?
                 model.FundingModel.GetFxRate(fwdDate, curve.Currency, euOpt.Currency) :
                 1.0;

            var df = model.FundingModel.GetDf(euOpt.DiscountCurve, model.BuildDate, euOpt.PaymentDate);

            if (euOpt.ExpiryDate < model.BuildDate) //expired, not yet paid
                return euOpt.Notional * df * (euOpt.CallPut == OptionType.Call ?
                    Max((fwd * fxFwd) - euOpt.Strike, 0) :
                    Max(euOpt.Strike - (fwd * fxFwd), 0));

            var vol = model.GetVolForStrikeAndDate(euOpt.AssetId, euOpt.ExpiryDate, euOpt.Strike / fxFwd);
            if (isCompo)
            {
                var fxId = $"{curve.Currency.Ccy}/{euOpt.PaymentCurrency.Ccy}";
                var fxVolFwd = model.FundingModel.GetFxRate(euOpt.ExpiryDate, curve.Currency, euOpt.PaymentCurrency);
                var fxVol = model.FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, euOpt.ExpiryDate, fxVolFwd);
                var tExpC = model.BuildDate.CalculateYearFraction(euOpt.ExpiryDate, DayCountBasis.Act365F);
                var correl = model.CorrelationMatrix.GetCorrelation(fxId, euOpt.AssetId, tExpC);
                vol = Sqrt(vol * vol + fxVol * fxVol + 2 * correl * fxVol * vol);
            }


            var tExp = model.BuildDate.CalculateYearFraction(euOpt.ExpiryDate.AddHours(18), DayCountBasis.Act365F, false);
            return BlackFunctions.BlackPV(fwd * fxFwd, euOpt.Strike, 0.0, tExp, vol, euOpt.CallPut) * euOpt.Notional * df;
        }

        public static double PV(this FxVanillaOption fxEuOpt, IAssetFxModel model, bool ignoreTodayFlows)
        {
            if (fxEuOpt.DeliveryDate < model.BuildDate || (ignoreTodayFlows && fxEuOpt.DeliveryDate == model.BuildDate))
                return 0.0;
            var df = model.FundingModel.GetDf(fxEuOpt.ForeignDiscountCurve, model.BuildDate, fxEuOpt.DeliveryDate);
            var fwdDate = fxEuOpt.Pair.SpotDate(fxEuOpt.ExpiryDate);
            var fwd = model.FundingModel.GetFxRate(fwdDate, fxEuOpt.PairStr);

            if (fxEuOpt.ExpiryDate < model.BuildDate) //expired, not yet paid
                return fxEuOpt.DomesticQuantity * df * (fxEuOpt.CallPut == OptionType.Call ?
                    Max(fwd  - fxEuOpt.Strike, 0) :
                    Max(fxEuOpt.Strike - fwd , 0));


            var vol = model.FundingModel.GetVolSurface(fxEuOpt.PairStr).GetVolForAbsoluteStrike(fxEuOpt.Strike, fxEuOpt.ExpiryDate, fwd);
            var t = model.BuildDate.CalculateYearFraction(fxEuOpt.DeliveryDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            return BlackFunctions.BlackPV(fwd, fxEuOpt.Strike, rf, t, vol, fxEuOpt.CallPut) * fxEuOpt.DomesticQuantity;
        }

        public static double PV(this EuropeanBarrierOption euBOpt, IAssetFxModel model, bool ignoreTodayFlows)
        {
            if (euBOpt.PaymentDate < model.BuildDate || (ignoreTodayFlows && euBOpt.PaymentDate == model.BuildDate))
                return 0.0;

            var df = model.FundingModel.GetDf(euBOpt.DiscountCurve, model.BuildDate, euBOpt.PaymentDate);
            var fwdDate = euBOpt.ExpiryDate.AddPeriod(RollType.F, euBOpt.FixingCalendar, euBOpt.SpotLag);
            var fwd = model.GetPriceCurve(euBOpt.AssetId).GetPriceForDate(fwdDate);

            if (euBOpt.ExpiryDate < model.BuildDate) //expired, not yet paid
                return euBOpt.Notional * df * (euBOpt.CallPut == OptionType.Call ?
                    Max(fwd - euBOpt.Strike, 0) :
                    Max(euBOpt.Strike - fwd, 0));

            var vol = model.GetVolForStrikeAndDate(euBOpt.AssetId, euBOpt.ExpiryDate, euBOpt.Strike);
            var t = model.BuildDate.CalculateYearFraction(euBOpt.PaymentDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            return BlackFunctions.BarrierOptionPV(fwd, euBOpt.Strike, rf, t, vol, euBOpt.CallPut, euBOpt.Barrier, euBOpt.BarrierType, euBOpt.BarrierSide);
        }

        public static double FlowsT0(this AsianOption asianOption, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            if (payDate != model.BuildDate)
                return 0.0;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            return asianOption.Notional * (asianOption.CallPut == OptionType.Call ? Max(0, FixedAverage - asianOption.Strike) : Max(0, asianOption.Strike - FixedAverage));
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

        public static double FlowsT0(this EuropeanOption euOpt, IAssetFxModel model)
        {
            if (euOpt.PaymentDate != model.BuildDate)
                return 0.0;

            var fixing = model.GetFixingDictionary(euOpt.AssetId).GetFixing(euOpt.ExpiryDate);
            return euOpt.Notional * (euOpt.CallPut == OptionType.Call ? Max(0, fixing - euOpt.Strike) : Max(0, euOpt.Strike - fixing));
        }

        public static double FlowsT0(this EuropeanBarrierOption euBOpt, IAssetFxModel model)
        {
            if (euBOpt.PaymentDate != model.BuildDate)
                return 0.0;

            var fixings = model.GetFixingDictionary(euBOpt.AssetId)
                .Where(x => x.Key >= euBOpt.BarrierObservationStartDate && x.Key <= euBOpt.BarrierObservationEndDate)
                .Select(x => x.Value);

            var barrierHit = (euBOpt.BarrierSide == BarrierSide.Up && fixings.Max() > euBOpt.Barrier) ||
                (euBOpt.BarrierSide == BarrierSide.Down && fixings.Min() < euBOpt.Barrier);

            var optionAlive = (barrierHit && euBOpt.BarrierType == BarrierType.KI) ||
                (!barrierHit && euBOpt.BarrierType == BarrierType.KO);

            return optionAlive ? ((EuropeanOption)euBOpt).FlowsT0(model) : 0.0;
        }


        public static double FlowsT0(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().FlowsT0(model);

        public static double FlowsT0(this AsianSwapStrip asianSwap, IAssetFxModel model) => asianSwap.Swaplets.Sum(x => x.FlowsT0(model));

        public static double FlowsT0(this AsianBasisSwap asianBasisSwap, IAssetFxModel model)
        {
            var payPV = asianBasisSwap.PaySwaplets.Sum(x => x.FlowsT0(model));
            var recPV = asianBasisSwap.RecSwaplets.Sum(x => x.FlowsT0(model));
            return payPV + recPV;
        }

        public static List<CashFlow> ExpectedCashFlows(this AsianSwap asianSwap, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianSwap.AssetId);

            var payDate = asianSwap.PaymentDate == DateTime.MinValue ?
             asianSwap.AverageEndDate.AddPeriod(asianSwap.PaymentLagRollType, asianSwap.PaymentCalendar, asianSwap.PaymentLag) :
             asianSwap.PaymentDate;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianSwap.GetAveragesForSwap(model);
            var fv = asianSwap.Notional * (FixedAverage - asianSwap.Strike);

            return new List<CashFlow>
            {
                new CashFlow
                {
                    Currency = asianSwap.Currency,
                    Notional = fv,
                    Fv = fv,
                    SettleDate = payDate
                }
            };
        }

        public static List<CashFlow> ExpectedCashFlows(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().ExpectedCashFlows(model);

        public static List<CashFlow> ExpectedCashFlows(this AsianSwapStrip asianSwap, IAssetFxModel model) => asianSwap.Swaplets.SelectMany(x => x.ExpectedCashFlows(model)).ToList();

        public static List<CashFlow> ExpectedCashFlows(this AsianBasisSwap asianBasisSwap, IAssetFxModel model) => 
            asianBasisSwap.PaySwaplets.SelectMany(x => x.ExpectedCashFlows(model)).Concat(
                    asianBasisSwap.RecSwaplets.SelectMany(x => x.ExpectedCashFlows(model))).ToList();


        public static List<CashFlow> ExpectedCashFlows(this EuropeanOption euOpt, IAssetFxModel model)
        {
            var fixing = euOpt.PaymentDate < model.BuildDate ?
                model.GetFixingDictionary(euOpt.AssetId).GetFixing(euOpt.ExpiryDate) :
                model.GetPriceCurve(euOpt.AssetId).GetPriceForFixingDate(euOpt.ExpiryDate);

            if(euOpt.Currency!= model.GetPriceCurve(euOpt.AssetId).Currency)
            {
                var fxRate = euOpt.PaymentDate < model.BuildDate ?
                    model.GetFixingDictionary(euOpt.FxFixingId).GetFixing(euOpt.ExpiryDate) :
                    model.FundingModel.GetFxRate(euOpt.ExpiryDate,euOpt.FxPair(model));
                fixing *= fxRate;
            }

            var fv = euOpt.Notional * (euOpt.CallPut == OptionType.Call ? Max(0, fixing - euOpt.Strike) : Max(0, euOpt.Strike - fixing));
            return new List<CashFlow>
            {
                new CashFlow
                {
                    Currency = euOpt.Currency,
                    Notional = fv,
                    Fv = fv,
                    SettleDate = euOpt.PaymentDate
                }
            };
        }

        public static List<CashFlow> ExpectedCashFlows(this AsianOption asianOption, IAssetFxModel model)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            var fv = asianOption.Notional * (asianOption.CallPut == OptionType.Call ? Max(0, FixedAverage - asianOption.Strike) : Max(0, asianOption.Strike - FixedAverage));

            return new List<CashFlow>
            {
                new CashFlow
                {
                    Currency = asianOption.Currency,
                    Notional = fv,
                    Fv = fv,
                    SettleDate = asianOption.PaymentDate
                }
            };
        }

        public static (double Financing, double Option) Theta(this AsianOption asianOption, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var curve = model.GetPriceCurve(asianOption.AssetId);

            var payDate = asianOption.PaymentDate == DateTime.MinValue ?
             asianOption.AverageEndDate.AddPeriod(asianOption.PaymentLagRollType, asianOption.PaymentCalendar, asianOption.PaymentLag) :
             asianOption.PaymentDate;

            if (payDate <= model.BuildDate)
                return (0.0, 0.0);

            //financing theta
            var dfAdj = model.FundingModel.GetDf(asianOption.Currency, model.BuildDate, fwdDate);
            var pV = asianOption.PV(model, true);
            var finTheta = pV * (1 - dfAdj);

            //"black" theta
            var (FixedAverage, FloatAverage, FixedCount, FloatCount) = asianOption.GetAveragesForSwap(model);
            var fwd = FloatAverage;
            var fixedAvg = FixedAverage;

            var surface = model.GetVolSurface(asianOption.AssetId);
            var isCompo = asianOption.PaymentCurrency != curve.Currency; //should reference vol surface, not curve

            var volDate = model.BuildDate.Max(asianOption.AverageStartDate).Average(asianOption.AverageEndDate);

            var adjustedStrike = asianOption.Strike;

            var discountCurve = model.FundingModel.GetCurve(asianOption.DiscountCurve);
            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            (var fwds, var todayFixed) = asianOption.GetFwdVector(model);
            var moneyness = adjustedStrike / FloatAverage;
            var sigmas = asianOption.FixingDates.Select((f, ix) => surface.GetVolForAbsoluteStrike(fwds[ix] * moneyness, f, fwds[ix])).ToArray();

            if (isCompo)
            {
                var fxId = $"{curve.Currency.Ccy}/{asianOption.PaymentCurrency.Ccy}";
                var fxPair = model.FundingModel.FxMatrix.GetFxPair(fxId);
                var correl = model.CorrelationMatrix.GetCorrelation(fxId, asianOption.AssetId);
                for (var i = 0; i < sigmas.Length; i++)
                {
                    var fxSpotDate = fxPair.SpotDate(asianOption.FixingDates[i]);
                    var fxFwd = model.FundingModel.GetFxRate(fxSpotDate, fxId);
                    var fxVol = model.FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, asianOption.FixingDates[i], fxFwd);
                    sigmas[i] = Sqrt(sigmas[i] * sigmas[i] + fxVol * fxVol + 2 * correl * fxVol * sigmas[i]);
                }
            }

            var blackTheta = TurnbullWakeman.Theta(fwds, asianOption.FixingDates, model.BuildDate, asianOption.PaymentDate, sigmas, asianOption.Strike, riskFree, asianOption.CallPut) * asianOption.Notional;
            blackTheta *= (fwdDate - model.BuildDate).TotalDays / 365.0;
            if (repCcy != asianOption.Currency)
            {
                var fxRate = model.FundingModel.GetFxRate(fwdDate, asianOption.Currency, repCcy);
                finTheta *= fxRate;
                blackTheta *= fxRate;
            }

            return (finTheta, blackTheta - finTheta);
        }

        public static (double Financing, double Option) Theta(this EuropeanOption euroOption, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var curve = model.GetPriceCurve(euroOption.AssetId);

            var payDate = euroOption.PaymentDate == DateTime.MinValue ?
             euroOption.ExpiryDate.AddPeriod(RollType.F, euroOption.PaymentCalendar, euroOption.PaymentLag) :
             euroOption.PaymentDate;

            if (payDate <= model.BuildDate)
                return (0.0, 0.0);

            //financing theta
            var dfAdj = model.FundingModel.GetDf(euroOption.Currency, model.BuildDate, fwdDate);
            var pV = euroOption.PV(model, true);
            var finTheta = pV * (1 - dfAdj);

            //"black" theta
            var isCompo = euroOption.Currency != model.GetPriceCurve(euroOption.AssetId).Currency;
            var fDate = euroOption.ExpiryDate.AddPeriod(RollType.F, euroOption.FixingCalendar, euroOption.SpotLag);
            var fwd = curve.GetPriceForDate(fDate);
            var fxFwd = isCompo ?
                 model.FundingModel.GetFxRate(fDate, curve.Currency, euroOption.Currency) :
                 1.0;

            var vol = model.GetVolForStrikeAndDate(euroOption.AssetId, euroOption.ExpiryDate, euroOption.Strike / fxFwd);
            if (isCompo)
            {
                var fxId = $"{curve.Currency.Ccy}/{euroOption.PaymentCurrency.Ccy}";
                var fxVolFwd = model.FundingModel.GetFxRate(fDate, curve.Currency, euroOption.PaymentCurrency);
                var fxVol = model.FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, euroOption.ExpiryDate, fxVolFwd);
                var correl = model.CorrelationMatrix.GetCorrelation(fxId, euroOption.AssetId);
                vol = Sqrt(vol * vol + fxVol * fxVol + 2 * correl * fxVol * vol);
            }

            var df = model.FundingModel.GetDf(euroOption.DiscountCurve, model.BuildDate, euroOption.PaymentDate);
            var t = model.BuildDate.CalculateYearFraction(euroOption.PaymentDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            var blackTheta = BlackFunctions.BlackTheta(fwd * fxFwd, euroOption.Strike, rf, t, vol, euroOption.CallPut) * euroOption.Notional;
            blackTheta *= (fwdDate - model.BuildDate).TotalDays / 365.0;
            if (repCcy != euroOption.Currency)
            {
                var fxRate = model.FundingModel.GetFxRate(fwdDate, euroOption.Currency, repCcy);
                finTheta *= fxRate;
                blackTheta *= fxRate;
            }

            return (finTheta, blackTheta - finTheta);
        }

        public static (double Financing, double Option) Theta(this FuturesOption fOpt, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var curve = model.GetPriceCurve(fOpt.AssetId);

            if (fOpt.ExpiryDate <= model.BuildDate)
                return (0.0, 0.0);

            //financing theta
            var finTheta = 0.0;
            if (fOpt.MarginingType == OptionMarginingType.Regular)
            {
                var dfAdj = model.FundingModel.GetDf(fOpt.Currency, model.BuildDate, fwdDate);
                var pV = fOpt.PV(model, true);
                finTheta = pV * (1 - dfAdj);
            }

            //"black" theta
            var fDate = fOpt.ExpiryDate;
            var fwd = curve.GetPriceForDate(fDate);
            var vol = model.GetVolForStrikeAndDate(fOpt.AssetId, fOpt.ExpiryDate, fOpt.Strike);

            var df = fOpt.MarginingType == OptionMarginingType.Regular ?
                model.FundingModel.GetDf(fOpt.DiscountCurve, model.BuildDate, fOpt.ExpiryDate) :
                1.0;
            var t = model.BuildDate.CalculateYearFraction(fOpt.ExpiryDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            var blackTheta = BlackFunctions.BlackTheta(fwd, fOpt.Strike, rf, t, vol, fOpt.CallPut) * fOpt.ContractQuantity * fOpt.LotSize;
            blackTheta *= (fwdDate - model.BuildDate).TotalDays / 365.0;
            if (repCcy != fOpt.Currency)
            {
                var fxRate = model.FundingModel.GetFxRate(fwdDate, fOpt.Currency, repCcy);
                finTheta *= fxRate;
                blackTheta *= fxRate;
            }

            return (finTheta, blackTheta - finTheta);
        }

        public static (double Financing, double Option) Theta(this FxVanillaOption fxOption, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var payDate = model.FundingModel.FxMatrix.GetFxPair(fxOption.FxPair(model)).SpotDate(fxOption.ExpiryDate);

            if (payDate <= model.BuildDate)
                return (0.0, 0.0);

            //financing theta
            var dfAdj = model.FundingModel.GetDf(fxOption.Currency, model.BuildDate, fwdDate);
            var pV = fxOption.PV(model, true);
            var finTheta = pV * (1 - dfAdj);

            //"black" theta
            var fwd = model.FundingModel.GetFxRate(payDate, fxOption.FxPair(model));
            var vol = model.GetFxVolForStrikeAndDate(fxOption.FxPair(model), fxOption.ExpiryDate, fxOption.Strike);

            var df = model.FundingModel.GetDf(fxOption.ForeignDiscountCurve, model.BuildDate, payDate);
            var t = model.BuildDate.CalculateYearFraction(payDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            var blackTheta = BlackFunctions.BlackTheta(fwd, fxOption.Strike, rf, t, vol, fxOption.CallPut) * fxOption.DomesticQuantity;
            blackTheta *= (fwdDate - model.BuildDate).TotalDays / 365.0;
            if (repCcy != fxOption.Currency)
            {
                var fxRate = model.FundingModel.GetFxRate(fwdDate, fxOption.Currency, repCcy);
                finTheta *= fxRate;
                blackTheta *= fxRate;
            }
            return (finTheta, blackTheta - finTheta);
        }

        public static (double Financing, double Option) Theta(this FxForward fxf, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var payDate = fxf.DeliveryDate;

            if (payDate <= model.BuildDate)
                return (0.0, 0.0);

            //financing theta
            //var dfAdjDom = model.FundingModel.GetDf(fxf.DomesticCCY, model.BuildDate, fwdDate);
            //var dfAdjFor = model.FundingModel.GetDf(fxf.ForeignCCY, model.BuildDate, fwdDate);
            //var fxDomRep = model.FundingModel.GetFxRate(fwdDate, fxf.DomesticCCY, repCcy);
            //var fxForRep = model.FundingModel.GetFxRate(fwdDate, fxf.ForeignCCY, repCcy);
            //var forQ = -fxf.DomesticQuantity * fxf.Strike;
            //var finTheta = fxf.DomesticQuantity * (1 - dfAdjDom) * fxDomRep + forQ * (1 - dfAdjFor) * fxForRep;
            var pv = fxf.Pv(model.FundingModel, false);
            var pvRep = pv * model.FundingModel.GetFxRate(fwdDate, fxf.ForeignCCY, repCcy);
            var dfAdjRep = model.FundingModel.GetDf(repCcy, model.BuildDate, fwdDate);
            var finTheta = pvRep * (1 - dfAdjRep);
            return (finTheta, 0.0);
        }

        public static (double Financing, double Option) Theta(this IInstrument ins, IAssetFxModel model, DateTime fwdDate, Currency repCcy)
        {
            var ccy = ins.GetCurrency();
            double pv;
            switch (ins)
            {
                case EuropeanOption euOpt:
                    return euOpt.Theta(model, fwdDate, repCcy);
                case AsianOption asOpt:
                    return asOpt.Theta(model, fwdDate, repCcy);
                case FuturesOption fOpt:
                    return fOpt.Theta(model, fwdDate, repCcy);
                case FxVanillaOption fxOpt:
                    return fxOpt.Theta(model, fwdDate, repCcy);
                case FxForward fxf:
                    return fxf.Theta(model, fwdDate, repCcy);
                case AsianSwap aSwp:
                    pv = aSwp.PV(model, true);
                    break;
                case AsianSwapStrip aSwpStrip:
                    pv = aSwpStrip.PV(model, true);
                    break;
                case AsianBasisSwap asianBasisSwap:
                    pv = asianBasisSwap.PV(model, true);
                    break;
                case Forward fwd:
                    pv = fwd.PV(model, true);
                    break;
                case Future fut:
                    return (0.0, 0.0);
                case ETC etc:
                    pv = etc.PV(model);
                    break;
                case CashBalance cash:
                    pv = cash.Pv(model.FundingModel, false);
                    break;
                case FixedRateLoanDeposit loanDepo:
                    pv = loanDepo.Pv(model.FundingModel, false);
                    break;
                case FloatingRateLoanDepo loanDepoFl:
                    pv = loanDepoFl.Pv(model.FundingModel, false);
                    break;
                case CashWrapper wrapper:
                    return Theta(wrapper.UnderlyingInstrument, model, fwdDate, repCcy);
                default:
                    throw new Exception("No theta logic defined for instrument type");
            }

            //financing theta
            var dfAdj = model.FundingModel.GetDf(ccy, model.BuildDate, fwdDate);
            var finTheta = pv * (1 - dfAdj);

            if (repCcy != ccy)
            {
                var fxRate = model.FundingModel.GetFxRate(fwdDate, ccy, repCcy);
                finTheta *= fxRate;
                var fxRate2 = model.FundingModel.GetFxRate(model.BuildDate, ccy, repCcy);
                finTheta += pv * (fxRate - fxRate2);
            }

            return (finTheta, 0.0);
        }

        public static ICube PV(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency = null, bool ignoreTodayFlows=false)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) },
                { "TradeType", typeof(string) },
                { "Portfolio", typeof(string) },
            };
            cube.Initialize(dataTypes);

            var pvs = new Tuple<Dictionary<string, object>, double>[portfolio.Instruments.Count];

            //for(var i=0;i< portfolio.Instruments.Count;i++)
            ParallelUtils.Instance.For(0, portfolio.Instruments.Count, 1, i =>
            {
                var (pv, ccy, tradeId, tradeType) = ComputePV(portfolio.Instruments[i], model, reportingCurrency, ignoreTodayFlows);

                var row = new Dictionary<string, object>
                  {
                        { "TradeId", tradeId },
                        { "Currency", ccy },
                        { "TradeType", tradeType },
                        { "Portfolio", portfolio.Instruments[i].PortfolioName??string.Empty },
                  };

                pvs[i] = new Tuple<Dictionary<string, object>, double>(row, pv);
            }, true).Wait();

            for (var i = 0; i < pvs.Length; i++)
            {
                cube.AddRow(pvs[i].Item1, pvs[i].Item2);
            }

            return cube;
        }

        public static double PVCapital(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency, HazzardCurve hazzardCurve, double LGD, double partyRiskWeight, Dictionary<string,string> assetToGroupMap, IIrCurve discountCurve, ICurrencyProvider currencyProvider, Dictionary<DateTime, IAssetFxModel> models=null)
        {
            var calcDates = portfolio.ExposureDatesForPortfolio(model.BuildDate);

            if (assetToGroupMap == null && portfolio.AssetIds().Length == 1)
            {
                var aid = portfolio.AssetIds().First();
                assetToGroupMap = new Dictionary<string, string>()
                {
                    { aid,aid }
                };
            }

            var calculator = new EADCalculator(portfolio, partyRiskWeight, assetToGroupMap, reportingCurrency, model, calcDates.ToArray(), currencyProvider);

            if (models==null)
                calculator.Process();
            else
                calculator.Process(models);

            var ead = calculator.ResultCube();

            var pvCapital = CapitalCalculator.PvCcrCapital_BII_SM(model.BuildDate, calcDates, calcDates.Select(d=>models[d]).ToArray(), portfolio, hazzardCurve, reportingCurrency, discountCurve, LGD);
            return pvCapital;
        }

        public static double GrossRoC(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency, HazzardCurve hazzardCurve, double LGD, double xVA_LGD, double cvaCapitalWeight, IIrCurve discountCurve, ICurrencyProvider currencyProvider, Dictionary<DateTime,IAssetFxModel> models)
        {
            var exposureDates = portfolio.ExposureDatesForPortfolio(model.BuildDate);
            var modelsForDates = exposureDates.Select(d => models[d]).ToArray();
            var pv = portfolio.PV(model, reportingCurrency).GetAllRows().Sum(r => r.Value);
            var cva = XVACalculator.CVA_Approx(exposureDates, portfolio, hazzardCurve, model, discountCurve, xVA_LGD, reportingCurrency, currencyProvider, models);
            var eads = CapitalCalculator.EAD_BII_SM(model.BuildDate, exposureDates, modelsForDates, portfolio, reportingCurrency);
            var ccrCapital = CapitalCalculator.PvCcrCapital_BII_SM(model.BuildDate, exposureDates, modelsForDates, portfolio, hazzardCurve, reportingCurrency, discountCurve, LGD, eads);            
            var cvaCapital = CapitalCalculator.PvCvaCapital_BII_SM(model.BuildDate, exposureDates, modelsForDates, portfolio, reportingCurrency, discountCurve, cvaCapitalWeight, eads);
            return (pv + cva) / (ccrCapital + cvaCapital);
        }

        private static DateTime[] ExposureDatesForPortfolio(this Portfolio portfolio, DateTime startDate)
        {
            var calcDates = new List<DateTime>();
            while (startDate < portfolio.LastSensitivityDate)
            {
                calcDates.Add(startDate);
                startDate = startDate.AddDays(14);
            }
            calcDates.Add(portfolio.LastSensitivityDate.AddDays(-1));
            return calcDates.Distinct().OrderBy(x => x).ToArray();
        }

        private static (double pv, string ccy, string tradeId, string tradeType) ComputePV(IInstrument ins, IAssetFxModel model, Currency reportingCurrency, bool ignoreTodayFlows=false)
        {
            var pv = 0.0;
            var fxRate = 1.0;
            var ccy = reportingCurrency?.ToString();
            var pvCcy = ins.GetCurrency();
            var tradeId = ins.TradeId;
            var tradeType = TradeType(ins);
            switch (ins)
            {
                case AsianOption asianOption:
                    pv = asianOption.PV(model, ignoreTodayFlows);
                    break;
                case AsianSwap swap:
                    pv = swap.PV(model, ignoreTodayFlows);
                    break;
                case AsianSwapStrip swapStrip:
                    pv = swapStrip.PV(model, ignoreTodayFlows);
                    break;
                case AsianBasisSwap basisSwap:
                    pv = basisSwap.PV(model, ignoreTodayFlows);
                    break;
                case EuropeanBarrierOption euBOpt:
                    pv = euBOpt.PV(model, ignoreTodayFlows);
                    break;
                case FxVanillaOption euFxOpt:
                    pv = euFxOpt.PV(model, ignoreTodayFlows);
                    break;
                case EuropeanOption euOpt:
                    pv = euOpt.PV(model, ignoreTodayFlows);
                    break;
                case Forward fwd:
                    pv = fwd.PV(model, ignoreTodayFlows);
                    break;
                case FuturesOption futOpt:
                    pv = futOpt.PV(model, ignoreTodayFlows);
                    break;
                case Future fut:
                    pv = fut.PV(model, ignoreTodayFlows);
                    break;
                case FxForward fxFwd:
                    pv = fxFwd.Pv(model.FundingModel, false, ignoreTodayFlows);
                    break;
                case FixedRateLoanDeposit loanDepo:
                    pv = loanDepo.Pv(model.FundingModel, true, ignoreTodayFlows);
                    break;
                case FloatingRateLoanDepo loanDepoFl:
                    pv = loanDepoFl.Pv(model.FundingModel, true, ignoreTodayFlows);
                    break;
                case CashBalance cash:
                    pv = cash.Pv(model.FundingModel, false);
                    break;
                case ETC etc:
                    pv = etc.PV(model);
                    break;
                case CashWrapper wrapper:
                    (pv, ccy, tradeId, tradeType) = ComputePV(wrapper.UnderlyingInstrument, model, pvCcy, ignoreTodayFlows);
                    if (reportingCurrency != null)
                        ccy = reportingCurrency.Ccy;
                    foreach(var cb in wrapper.CashBalances)
                    {
                        var p = ComputePV(cb, model, pvCcy);
                        pv += p.pv;
                    }
                    break;
                default:
                    throw new Exception($"Unabled to handle product of type {ins.GetType()}");
            }

            if (reportingCurrency != null)
                fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, pvCcy);
            else
                ccy = pvCcy.ToString();

            return (pv / fxRate, ccy, tradeId, tradeType);
        }

        private static Currency GetCurrency(this IInstrument ins)
        {
            switch(ins)
            {
                case CashWrapper wrapper:
                    return wrapper.UnderlyingInstrument.GetCurrency();
                case FxForward fxf:
                    return fxf.ForeignCCY;
                case IAssetInstrument aIns:
                    return aIns.PaymentCurrency;
                default:
                    throw new Exception("Unable to determine instrument currency");
                
            }
        }

        private static (double flow, string tradeId, string tradeType, string ccy) ComputeFlowsT0(IInstrument ins, IAssetFxModel model, Currency reportingCurrency)
        {
            var flow = 0.0;
            var fxRate = 1.0;
            var tradeId = ins.TradeId;
            var tradeType = TradeType(ins);
            var flowCcy = ins.GetCurrency();
            var ccy = reportingCurrency?.ToString();
            switch (ins)
            {
                case AsianOption asianOption:
                    flow = asianOption.FlowsT0(model);
                    break;
                case AsianSwap swap:
                    flow = swap.FlowsT0(model);
                    break;
                case AsianSwapStrip swapStrip:
                    flow = swapStrip.FlowsT0(model);
                    break;
                case AsianBasisSwap basisSwap:
                    flow = basisSwap.FlowsT0(model);
                    break;
                case EuropeanBarrierOption euBOpt:
                    flow = euBOpt.FlowsT0(model);
                    break;
                case EuropeanOption euOpt:
                    flow = euOpt.FlowsT0(model);
                    break;
                case Forward fwd:
                    flow = fwd.FlowsT0(model);
                    break;
                case FuturesOption futOpt:
                    flow = futOpt.FlowsT0(model);
                    break;
                case Future fut:
                    flow = fut.FlowsT0(model);
                    break;
                case FxForward fxFwd:
                    flow = fxFwd.FlowsT0(model.FundingModel);
                    break;
                case FixedRateLoanDeposit loanDepo:
                    flow = loanDepo.FlowsT0(model.FundingModel);
                    break;
                case CashBalance cash:
                case ETC etc:
                    flow = 0;
                    break;
                case CashWrapper wrapper:
                    (flow, tradeId, tradeType, ccy) = ComputeFlowsT0(wrapper.UnderlyingInstrument, model, flowCcy);
                    foreach (var cb in wrapper.CashBalances)
                    {
                        var p = ComputeFlowsT0(cb, model, flowCcy);
                        flow += p.flow;
                    }
                    break;
                default:
                    throw new Exception($"Unabled to handle product of type {ins.GetType()}");
            }

            if (reportingCurrency != null)
                fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, flowCcy);
            else
                ccy = flowCcy.ToString();            

            return (flow / fxRate, tradeId, tradeType, ccy);
        }

        private static (List<CashFlow> flows, string tradeId, string tradeType) ComputeExpectedCashFlows(IInstrument ins, IAssetFxModel model)
        {
            var flows = new List<CashFlow>();
            var tradeId = ins.TradeId;
            var tradeType = TradeType(ins);

            switch (ins)
            {
                case IFundingInstrument fi:
                    flows = fi.ExpectedCashFlows(model);
                    break;
                case AsianOption asianOption:
                    flows = asianOption.ExpectedCashFlows(model);
                    break;
                case AsianSwap swap:
                    flows = swap.ExpectedCashFlows(model);
                    break;
                case AsianSwapStrip swapStrip:
                    flows = swapStrip.ExpectedCashFlows(model);
                    break;
                case AsianBasisSwap basisSwap:
                    flows = basisSwap.ExpectedCashFlows(model);
                    break;
                case EuropeanBarrierOption euBOpt:
                    flows = euBOpt.ExpectedCashFlows(model);
                    break;
                case EuropeanOption euOpt:
                    flows = euOpt.ExpectedCashFlows(model);
                    break;
                case Forward fwd:
                    flows = fwd.ExpectedCashFlows(model);
                    break;
                case CashWrapper wrapper:
                    (flows, tradeId, tradeType) = ComputeExpectedCashFlows(wrapper.UnderlyingInstrument, model);
                    flows = flows.Concat(wrapper.CashBalances.SelectMany(cb => cb.ExpectedCashFlows(model))).ToList();
                    break;
                    
                default:
                    //do nothing
                    break;
            }

            return (flows, tradeId, tradeType);
        }


        public static string TradeType(this IInstrument ins)
        {
            string tradeType;
            switch (ins)
            {
                case AsianOption asianOption:
                    tradeType = "AsianOption";
                    break;
                case AsianSwap swap:
                    tradeType = "AsianSwap";
                    break;
                case AsianSwapStrip swapStrip:
                    tradeType = "AsianSwapStrip";
                    break;
                case AsianBasisSwap basisSwap:
                    tradeType = "AsianBasisSwap";
                    break;
                case EuropeanBarrierOption euBOpt:
                    tradeType = "BarrierOption";
                    break;
                case FxVanillaOption euFxOpt:
                    tradeType = "EuropeanOption";
                    break;
                case EuropeanOption euOpt:
                    tradeType = "EuropeanOption";
                    break;
                case Forward fwd:
                    tradeType = "Forward";
                    break;
                case FuturesOption futOpt:
                    tradeType = "FutureOption";
                    break;
                case Future fut:
                    tradeType = "Future";
                    break;
                case FxForward fxFwd:
                    tradeType = "FxForward";
                    break;
                case FixedRateLoanDeposit loanDepo:
                case FloatingRateLoanDepo loanDepoF:
                    tradeType = "LoanDepo";
                    break;
                case PhysicalBalance phys:
                    tradeType = "Physical";
                    break;
                case CashBalance cash:
                    tradeType = "Cash";
                    break;
                case AsianLookbackOption lbo:
                    tradeType = "LookBack";
                    break;
                case BackPricingOption bpo:
                case MultiPeriodBackpricingOption mpbpo:
                    tradeType = "BackPricing";
                    break;
                case ETC etc:
                    tradeType = "ETC";
                    break;
                case OneTouchOption oto:
                    tradeType = "OneTouch";
                    break;
                case DoubleNoTouchOption dnt:
                    tradeType = "DoubleNoTouch";
                    break;
                case CashWrapper wrapper:
                    tradeType = TradeType(wrapper.UnderlyingInstrument);
                    break;
                default:
                    throw new Exception($"Unable to handle product of type {ins.GetType()}");
            }
            return tradeType;
        }

        public static ICube FlowsT0(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) },
                { "TradeType", typeof(string) },
            };
            cube.Initialize(dataTypes);

            foreach (var ins in portfolio.Instruments)
            {
                var (flow, tradeId, tradeType, ccy) = ComputeFlowsT0(ins, model, reportingCurrency);
                
                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy },
                    { "TradeType", tradeType }
                };
                cube.AddRow(row, flow);
            }

            return cube;
        }

        public static ICube ExpectedCashFlows(this Portfolio portfolio, IAssetFxModel model)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) },
                { "TradeType", typeof(string) },
                { "PayDate", typeof(DateTime) },
            };
            cube.Initialize(dataTypes);

            foreach (var ins in portfolio.Instruments)
            {
                var (flows, tradeId, tradeType) = ComputeExpectedCashFlows(ins, model);

                foreach (var flow in flows.Where(f => f.SettleDate >= model.BuildDate))
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", tradeId },
                        { "Currency", flow.Currency.Ccy },
                        { "TradeType", tradeType },
                        { "PayDate", flow.SettleDate }
                    };
                    cube.AddRow(row, flow.Fv);
                }
            }

            return cube;
        }


        public static ICube AssetDelta(this Portfolio portfolio, IAssetFxModel model)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetDelta(false);
        }

        public static ICube FxDelta(this Portfolio portfolio, IAssetFxModel model, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false, bool reportInverse=true)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.FxDelta(homeCcy, currencyProvider, computeGamma, reportInverse);
        }

        public static ICube FxDeltaRaw(this Portfolio portfolio, IAssetFxModel model, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false, bool reportInverse = true)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.FxDeltaRaw(homeCcy, currencyProvider, computeGamma);
        }

        public static ICube AssetDeltaGamma(this Portfolio portfolio, IAssetFxModel model)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetDelta(true);
        }


        public static ICube AssetVega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetVega(reportingCcy);

        }

        public static ICube AssetSegaRega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetSegaRega(reportingCcy);

        }

        public static ICube FxVega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.FxVega(reportingCcy);

        }

        public static ICube AssetIrDelta(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy = null, double bumpSize = 0.0001)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetIrDelta(reportingCcy, bumpSize);

        }

        public static ICube CorrelationDelta(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy, double epsilon)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.CorrelationDelta(reportingCcy, epsilon);
        }

        public static ICube AssetTheta(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetThetaCharm(fwdValDate, reportingCcy, currencyProvider, false);
        }

        public static ICube AssetAnalyticTheta(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var m = model.Clone();
            m.AttachPortfolio(portfolio);
            return m.AssetAnalyticTheta(fwdValDate, reportingCcy, currencyProvider, false);
        }

        public static IAssetFxModel RollModel(this IAssetFxModel model, DateTime fwdValDate, ICurrencyProvider currencyProvider)
        {
            if (model.BuildDate == fwdValDate)
                return model.Clone();

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
                if (surface.Value is RiskyFlySurface rf)
                    rolledFxVolSurfaces.Add(surface.Key, rf.RollSurface(fwdValDate));
                else if (surface.Value is GridVolSurface g)
                    rolledFxVolSurfaces.Add(surface.Key, g.RollSurface(fwdValDate));
                else
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
                var surface = model.GetVolSurface(surfaceName);

                if (surface is RiskyFlySurface rf)
                    rolledVolSurfaces.Add(surfaceName, rf.RollSurface(fwdValDate));
                else if (surface is GridVolSurface g)
                    rolledVolSurfaces.Add(surfaceName, g.RollSurface(fwdValDate));
                else
                    rolledVolSurfaces.Add(surfaceName, surface);
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
                            var curve = model.GetPriceCurve(newDict.AssetId);
                            var estFixing = curve.GetPriceForDate(date.AddPeriod(RollType.F, curve.SpotCalendar, curve.SpotLag));
                            newDict.Add(date, estFixing);
                        }
                        else //its FX
                        {
                            var id = newDict.FxPair ?? newDict.AssetId;
                            var ccyLeft = currencyProvider[id.Substring(0, 3)];
                            var ccyRight = currencyProvider[id.Substring(id.Length - 3, 3)];
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
