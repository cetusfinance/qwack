using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Descriptors;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options;
using Qwack.Options.Asians;
using Qwack.Utils.Parallel;
using static System.Math;

namespace Qwack.Models.Models
{
    public static class AssetProductEx
    {
        private static readonly bool _useFuturesMethod = false;

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

                if(adjustedStrike<0) //its delta-1
                {
                    var avg = (FixedAverage * FixedCount + FloatAverage * FloatCount) / (FixedCount + FloatCount);
                    return asianOption.CallPut == OptionType.Put ? 0.0 : (avg - asianOption.Strike) * asianOption.Notional;
                }
            }

            var discountCurve = model.FundingModel.GetCurve(asianOption.DiscountCurve);
            var riskFree = discountCurve.GetForwardRate(discountCurve.BuildDate, asianOption.PaymentDate, RateType.Exponential, DayCountBasis.Act365F);

            if (_useFuturesMethod)
            {
                var fwds = asianOption.GetFwdVector(model);
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

                return TurnbullWakeman.PV(fwds, asianOption.FixingDates, model.BuildDate, asianOption.PaymentDate, sigmas, asianOption.Strike, riskFree, asianOption.CallPut) * asianOption.Notional;
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

        public static double[] GetFwdVector(this AsianSwap swap, IAssetFxModel model)
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
                if (swap.FixingDates[i] < model.BuildDate || fixingForToday)
                    fwds[i] = fixingDict.GetFixing(swap.FixingDates[i]);
                else
                    fwds[i] = priceCurve.GetPriceForDate(swap.FixingDates[i].AddPeriod(swap.SpotLagRollType, swap.FixingCalendar, swap.SpotLag));
            }


            if (swap.PaymentCurrency == priceCurve.Currency)
                return fwds;
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
                    if (swap.FixingDates[i] < model.BuildDate || fixingForTodayFx)
                        fxRates[i] = fxFixingDict.GetFixing(fxDates[i]);
                    else

                        fxRates[i] = model.FundingModel.GetFxRate(fxPair.SpotDate(fxDates[i]), priceCurve.Currency, swap.PaymentCurrency);
                }

                if(swap.FxConversionType==FxConversionType.AverageThenConvert)
                {
                    var fxAvg = fxRates.Average();
                    return fwds.Select(f => f * fxAvg).ToArray();
                }
                else
                {
                    return fwds.Select((f,ix) => f * fxRates[ix]).ToArray();
                }
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

        public static double PV(this FuturesOption option, IAssetFxModel model)
        {
            if (option.ExpiryDate < model.BuildDate)
                return 0.0;

            if (!(option.ExerciseType == OptionExerciseType.European ||
                (option.ExerciseType == OptionExerciseType.American && option.MarginingType == OptionMarginingType.FuturesStyle)
                ))
                throw new Exception("Only European style options currently supported");

            var price = model.GetPriceCurve(option.AssetId).GetPriceForDate(option.ExpiryDate);
            var df = option.MarginingType == OptionMarginingType.FuturesStyle ? 1.0
                : model.FundingModel.GetDf(option.DiscountCurve, model.BuildDate, option.ExpiryDate);
            var t = model.BuildDate.CalculateYearFraction(option.ExpiryDate, DayCountBasis.Act365F);
            var vol = model.GetVolForStrikeAndDate(option.AssetId, option.ExpiryDate, option.Strike);


            var fv = BlackFunctions.BlackPV(price, option.Strike, 0.0, t, vol, option.CallPut);
            return fv * df * option.ContractQuantity * option.LotSize;
        }

        public static double PV(this Forward fwd, IAssetFxModel model) => fwd.AsBulletSwap().PV(model);

        public static double PV(this EuropeanOption euOpt, IAssetFxModel model)
        {
            if (euOpt.ExpiryDate < model.BuildDate)
                return 0.0;

            var fwdDate = euOpt.ExpiryDate.AddPeriod(RollType.F, euOpt.FixingCalendar, euOpt.SpotLag);
            var fwd = model.GetPriceCurve(euOpt.AssetId).GetPriceForDate(fwdDate);
            var vol = model.GetVolForStrikeAndDate(euOpt.AssetId, euOpt.ExpiryDate, euOpt.Strike);
            var df = model.FundingModel.GetDf(euOpt.DiscountCurve, model.BuildDate, euOpt.PaymentDate);
            var t = model.BuildDate.CalculateYearFraction(euOpt.PaymentDate, DayCountBasis.Act365F);
            var rf = Log(1 / df) / t;
            return BlackFunctions.BlackPV(fwd, euOpt.Strike, rf, t, vol, euOpt.CallPut);
        }

        public static double PV(this EuropeanBarrierOption euBOpt, IAssetFxModel model)
        {
            if (euBOpt.ExpiryDate < model.BuildDate)
                return 0.0;

            var fwdDate = euBOpt.ExpiryDate.AddPeriod(RollType.F, euBOpt.FixingCalendar, euBOpt.SpotLag);
            var fwd = model.GetPriceCurve(euBOpt.AssetId).GetPriceForDate(fwdDate);
            var vol = model.GetVolForStrikeAndDate(euBOpt.AssetId, euBOpt.ExpiryDate, euBOpt.Strike);
            var df = model.FundingModel.GetDf(euBOpt.DiscountCurve, model.BuildDate, euBOpt.PaymentDate);
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

        public static ICube PV(this Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency = null)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) },
                { "TradeType", typeof(string) },
            };
            cube.Initialize(dataTypes);

            var pvs = new Tuple<Dictionary<string, object>, double>[portfolio.Instruments.Count];

            //ParallelUtils.Instance.For(0, portfolio.Instruments.Count, 1, i =>
            for(var i=0;i< portfolio.Instruments.Count;i++)
            {
                var ins = portfolio.Instruments[i];
                var pv = 0.0;
                var fxRate = 1.0;
                string tradeId = null;
                var ccy = reportingCurrency?.ToString();
                string tradeType = null;
                switch (ins)
                {
                    case AsianOption asianOption:
                        tradeType = "AsianOption";
                        pv = asianOption.PV(model);
                        tradeId = asianOption.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, asianOption.PaymentCurrency);
                        else
                            ccy = asianOption.PaymentCurrency.ToString();
                        break;
                    case AsianSwap swap:
                        tradeType = "AsianSwap";
                        pv = swap.PV(model);
                        tradeId = swap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swap.PaymentCurrency);
                        else
                            ccy = swap.PaymentCurrency.ToString();
                        break;
                    case AsianSwapStrip swapStrip:
                        tradeType = "AsianSwapStrip";
                        pv = swapStrip.PV(model);
                        tradeId = swapStrip.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swapStrip.Swaplets.First().PaymentCurrency);
                        else
                            ccy = swapStrip.Swaplets.First().PaymentCurrency.ToString();
                        break;
                    case AsianBasisSwap basisSwap:
                        tradeType = "AsianBasisSwap";
                        pv = basisSwap.PV(model);
                        tradeId = basisSwap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, basisSwap.PaySwaplets.First().PaymentCurrency);
                        else
                            ccy = basisSwap.PaySwaplets.First().PaymentCurrency.ToString();
                        break;
                    case EuropeanBarrierOption euBOpt:
                        tradeType = "BarrierOption";
                        pv = euBOpt.PV(model);
                        tradeId = euBOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, euBOpt.PaymentCurrency);
                        else
                            ccy = euBOpt.PaymentCurrency.ToString();
                        break;
                    case EuropeanOption euOpt:
                        tradeType = "EuropeanOption";
                        pv = euOpt.PV(model);
                        tradeId = euOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, euOpt.PaymentCurrency);
                        else
                            ccy = euOpt.PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        tradeType = "Forward";
                        pv = fwd.PV(model);
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    case FuturesOption futOpt:
                        tradeType = "FutureOption";
                        pv = futOpt.PV(model);
                        tradeId = futOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, futOpt.Currency);
                        else
                            ccy = futOpt.Currency.ToString();
                        break;
                    case Future fut:
                        tradeType = "Future";
                        pv = fut.PV(model);
                        tradeId = fut.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fut.Currency);
                        else
                            ccy = fut.Currency.ToString();
                        break;
                    case FxForward fxFwd:
                        tradeType = "FxForward";
                        pv = fxFwd.Pv(model.FundingModel, false);
                        tradeId = fxFwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        tradeType = "LoanDepo";
                        pv = loanDepo.Pv(model.FundingModel, false);
                        tradeId = loanDepo.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
                        break;
                    case CashBalance cash:
                        tradeType = "Cash";
                        pv = cash.Pv(model.FundingModel, false);
                        tradeId = cash.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, cash.Ccy);
                        else
                            ccy = cash.Ccy.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }

                var row = new Dictionary<string, object>
                  {
                        { "TradeId", tradeId },
                        { "Currency", ccy },
                        { "TradeType", tradeType },
                  };

                pvs[i] = new Tuple<Dictionary<string, object>, double>(row, pv / fxRate);
            }
            //, true).Wait();

            for (var i = 0; i < pvs.Length; i++)
            {
                cube.AddRow(pvs[i].Item1, pvs[i].Item2);
            }

            return cube;
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
                var flow = 0.0;
                var fxRate = 1.0;
                string tradeId = null;
                string tradeType = null;
                var ccy = reportingCurrency?.ToString();
                switch (ins)
                {
                    case AsianOption asianOption:
                        tradeType = "AsianOption";
                        flow = asianOption.FlowsT0(model);
                        tradeId = asianOption.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, asianOption.PaymentCurrency);
                        else
                            ccy = asianOption.PaymentCurrency.ToString();
                        break;
                    case AsianSwap swap:
                        tradeType = "AsianSwap";
                        flow = swap.FlowsT0(model);
                        tradeId = swap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swap.PaymentCurrency);
                        else
                            ccy = swap.PaymentCurrency.ToString();
                        break;
                    case AsianSwapStrip swapStrip:
                        tradeType = "AsianSwapStrip";
                        flow = swapStrip.FlowsT0(model);
                        tradeId = swapStrip.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, swapStrip.Swaplets.First().PaymentCurrency);
                        else
                            ccy = swapStrip.Swaplets.First().PaymentCurrency.ToString();
                        break;
                    case AsianBasisSwap basisSwap:
                        tradeType = "AsianBasisSwap";
                        flow = basisSwap.FlowsT0(model);
                        tradeId = basisSwap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, basisSwap.PaySwaplets.First().PaymentCurrency);
                        else
                            ccy = basisSwap.PaySwaplets.First().PaymentCurrency.ToString();
                        break;
                    case EuropeanBarrierOption euBOpt:
                        tradeType = "BarrierOption";
                        flow = euBOpt.FlowsT0(model);
                        tradeId = euBOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, euBOpt.PaymentCurrency);
                        else
                            ccy = euBOpt.PaymentCurrency.ToString();
                        break;
                    case EuropeanOption euOpt:
                        tradeType = "EuropeanOption";
                        flow = euOpt.FlowsT0(model);
                        tradeId = euOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, euOpt.PaymentCurrency);
                        else
                            ccy = euOpt.PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        tradeType = "Forward";
                        flow = fwd.FlowsT0(model);
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    case FuturesOption futOpt:
                        tradeType = "FutureOption";
                        flow = futOpt.FlowsT0(model);
                        tradeId = futOpt.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, futOpt.Currency);
                        else
                            ccy = futOpt.Currency.ToString();
                        break;
                    case Future fut:
                        tradeType = "Future";
                        flow = fut.FlowsT0(model);
                        tradeId = fut.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fut.Currency);
                        else
                            ccy = fut.Currency.ToString();
                        break;
                    case FxForward fxFwd:
                        tradeType = "FxForward";
                        flow = fxFwd.FlowsT0(model.FundingModel);
                        tradeId = fxFwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        tradeType = "LoanDepo";
                        flow = loanDepo.FlowsT0(model.FundingModel);
                        tradeId = loanDepo.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
                        break;
                    case CashBalance cash:
                        tradeType = "Cash";
                        flow = 0;
                        tradeId = cash.TradeId;
                        if (reportingCurrency != null)
                            fxRate = model.FundingModel.GetFxRate(model.BuildDate, reportingCurrency, cash.Ccy);
                        else
                            ccy = cash.Ccy.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }


                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy },
                    { "TradeType", tradeType }
                };
                cube.AddRow(row, flow / fxRate);
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
                { "TradeType",  typeof(string) },
                { "AssetId", typeof(string) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            foreach (var curveName in model.CurveNames)
            {
                var curveObj = model.GetPriceCurve(curveName);
                var linkedCurves = model.Curves
                    .Where(x => x is BasisPriceCurve bp && bp.BaseCurve.Name == curveName)
                    .Select(x => x.Name);
                var prevCount = 0;
                while (linkedCurves.Count() != prevCount)
                {
                    prevCount = linkedCurves.Count();
                    var newBaseCurves = new List<string>();
                    foreach (var depCurve in linkedCurves)
                    {
                        var baseCurve = model.GetPriceCurve(depCurve);
                        newBaseCurves.Add(depCurve);
                    }

                    var newlinkedCurves = model.Curves
                        .Where(x => x is BasisPriceCurve bp && newBaseCurves.Contains(bp.BaseCurve.Name))
                        .Select(x => x.Name);
                    linkedCurves = linkedCurves.Concat(newlinkedCurves).Distinct();
                }

                var subPortfolio = new Portfolio()
                {
                    Instruments = portfolio.Instruments
                    .Where(x => (x is IAssetInstrument ia) && 
                    (ia.AssetIds.Contains(curveObj.AssetId) || ia.AssetIds.Any(aid=>linkedCurves.Contains(aid))))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var pvCube = subPortfolio.PV(model, curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex("TradeId");
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

                var bumpedCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);

                var results = new List<Tuple<Dictionary<string, object>, double>>[bumpedCurves.Count];
                var bcList = bumpedCurves.ToList();

                ParallelUtils.Instance.For(0, results.Length, 1, ii =>
                {
                    results[ii] = new List<Tuple<Dictionary<string, object>, double>>();

                    var bCurve = bcList[ii];
                    var newModel = model.Clone();
                    newModel.AddPriceCurve(curveName, bCurve.Value);

                    var dependentCurves = model.Curves.Where(x => x is BasisPriceCurve bp && bp.BaseCurve.Name == curveName);
                    while (dependentCurves.Any())
                    {
                        var newBaseCurves = new List<string>();
                        foreach (BasisPriceCurve depCurve in dependentCurves)
                        {
                            var baseCurve = newModel.GetPriceCurve(depCurve.BaseCurve.Name);
                            var recalCurve = depCurve.ReCalibrate(baseCurve);
                            newModel.AddPriceCurve(depCurve.Name, recalCurve);
                            newBaseCurves.Add(depCurve.Name);
                        }

                        dependentCurves = model.Curves.Where(x => x is BasisPriceCurve bp && newBaseCurves.Contains(bp.BaseCurve.Name));
                    }

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
                                { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
                                { "AssetId", curveName },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "Delta" }

                            };
                            results[ii].Add(new Tuple<Dictionary<string, object>, double>(row, delta));
                        }
                    }
                }).Wait();

                for (var i = 0; i < results.Length; i++)
                    for (var j = 0; j < results[i].Count(); j++)
                    {
                        if (results[i][j].Item1 != null)
                            cube.AddRow(results[i][j].Item1, results[i][j].Item2);
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

        public static ICube FxDelta(this Portfolio portfolio, IAssetFxModel model, Currency homeCcy, ICurrencyProvider currencyProvider, bool computeGamma = false)
        {
            var bumpSize = 0.0001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
                { "AssetId", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);
            var mf = model.FundingModel.DeepClone(null);

            var domCcy = model.FundingModel.FxMatrix.BaseCurrency;

            if (homeCcy != null && homeCcy != domCcy)//remap onto new base currency
            {
                domCcy = homeCcy;
                var homeToBase = mf.FxMatrix.SpotRates[homeCcy];
                var ccys = mf.FxMatrix.SpotRates.Keys.ToList()
                    .Concat(new[] { mf.FxMatrix.BaseCurrency })
                    .Where(x => x != homeCcy);
                var newRateDict = new Dictionary<Currency, double>();
                foreach (var ccy in ccys)
                {
                    var spotDate = mf.FxMatrix.GetFxPair(homeCcy, ccy).SpotDate(mf.BuildDate);
                    var newRate = mf.GetFxRate(spotDate, homeCcy, ccy);
                    newRateDict.Add(ccy, newRate);
                }

                var newFx = new FxMatrix(currencyProvider);
                newFx.Init(homeCcy, mf.FxMatrix.BuildDate, newRateDict, mf.FxMatrix.FxPairDefinitions, mf.FxMatrix.DiscountCurveMap);
                mf.SetupFx(newFx);
            }

            var m = model.Clone(mf);

            foreach (var currency in m.FundingModel.FxMatrix.SpotRates.Keys)
            {
                var pvCube = portfolio.PV(m, m.FundingModel.FxMatrix.BaseCurrency);
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

                var fxPair = $"{currency}/{domCcy}";

                var newModel = m.Clone();
                var bumpedSpot = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 + bumpSize);
                newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpot;
                var inverseSpotBump = 1 / bumpedSpot - 1 / m.FundingModel.FxMatrix.SpotRates[currency];

                var bumpedPVCube = portfolio.PV(newModel, m.FundingModel.FxMatrix.BaseCurrency);
                var bumpedRows = bumpedPVCube.GetAllRows();
                if (bumpedRows.Length != pvRows.Length)
                    throw new Exception("Dimensions do not match");

                ResultCubeRow[] bumpedRowsDown = null;
                var inverseSpotBumpDown = 0.0;

                if (computeGamma)
                {
                    var bumpedSpotDown = m.FundingModel.FxMatrix.SpotRates[currency] * (1.00 - bumpSize);
                    newModel.FundingModel.FxMatrix.SpotRates[currency] = bumpedSpotDown;
                    inverseSpotBumpDown = 1 / bumpedSpotDown - 1 / m.FundingModel.FxMatrix.SpotRates[currency];

                    var bumpedPVCubeDown = portfolio.PV(newModel, m.FundingModel.FxMatrix.BaseCurrency);
                    bumpedRowsDown = bumpedPVCubeDown.GetAllRows();
                    if (bumpedRowsDown.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                }

                for (var i = 0; i < bumpedRows.Length; i++)
                {
                    var delta = (bumpedRows[i].Value - pvRows[i].Value) / inverseSpotBump;

                    if (delta != 0.0)
                    {
                        var row = new Dictionary<string, object>
                        {
                            { "TradeId", bumpedRows[i].MetaData[tidIx] },
                            { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
                            { "AssetId", fxPair },
                            { "Metric", "FxSpotDelta" }
                        };
                        cube.AddRow(row, delta);
                    }

                    if (computeGamma)
                    {
                        var deltaDown = (bumpedRowsDown[i].Value - pvRows[i].Value) / inverseSpotBumpDown;
                        var gamma = (delta - deltaDown) / (inverseSpotBump + inverseSpotBumpDown) * 2.0;
                        if (gamma != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
                                { "AssetId", fxPair },
                                { "Metric", "FxSpotGamma" }
                            };
                            cube.AddRow(row, delta);
                        }
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
                { "TradeType", typeof(string) },
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
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var bumpedUpCurves = curveObj.GetDeltaScenarios(bumpSize, lastDateInBook);
                var bumpedDownCurves = curveObj.GetDeltaScenarios(-bumpSize, lastDateInBook);

                var resultsD = new List<Tuple<Dictionary<string, object>, double>>[bumpedUpCurves.Count];
                var resultsG = new List<Tuple<Dictionary<string, object>, double>>[bumpedUpCurves.Count];
                var bcList = bumpedUpCurves.ToList();
                ParallelUtils.Instance.For(0, resultsD.Length, 1, ii =>
                {
                    resultsD[ii] = new List<Tuple<Dictionary<string, object>, double>>();
                    resultsG[ii] = new List<Tuple<Dictionary<string, object>, double>>();

                    var bUpCurve = bcList[ii];

                    //    foreach (var bUpCurve in bumpedUpCurves)
                    //{
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
                        var deltaDown = ((double)pvRows[i].Value - (double)bumpedDownRows[i].Value) / bumpSize;

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

                            if (delta != 0.0)
                            {
                                var row = new Dictionary<string, object>
                                {
                                    { "TradeId", bumpedUpRows[i].MetaData[tidIx] },
                                    { "TradeType", bumpedUpRows[i].MetaData[tTypeIx] },
                                    { "AssetId", curveName },
                                    { "PointLabel", bUpCurve.Key },
                                    { "Metric", "Delta" }
                                };
                                resultsD[ii].Add(new Tuple<Dictionary<string, object>, double>(row, delta));
                                //cube.AddRow(row, delta);
                            }
                            if (Abs(gamma) > 1e-12)
                            {
                                var rowG = new Dictionary<string, object>
                                {
                                    { "TradeId", bumpedUpRows[i].MetaData[tidIx] },
                                    { "TradeType", bumpedUpRows[i].MetaData[tTypeIx] },
                                    { "AssetId", curveName },
                                    { "PointLabel", bUpCurve.Key },
                                    { "Metric", "Gamma" }
                                };
                                resultsG[ii].Add(new Tuple<Dictionary<string, object>, double>(rowG, gamma));
                                //cube.AddRow(rowG, gamma);
                            }
                        }
                    }
                }).Wait();


                for (var i = 0; i < resultsD.Length; i++)
                    for (var j = 0; j < resultsD[i].Count(); j++)
                    {
                        if (resultsD[i][j].Item1 != null)
                            cube.AddRow(resultsD[i][j].Item1, resultsD[i][j].Item2);
                    }
                for (var i = 0; i < resultsG.Length; i++)
                    for (var j = 0; j < resultsG[i].Count(); j++)
                    {
                        if (resultsG[i][j].Item1 != null)
                            cube.AddRow(resultsG[i][j].Item1, resultsG[i][j].Item2);
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
                { "TradeType", typeof(string) },
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
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

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
                        var vega = ((double)bumpedRows[i].Value - (double)pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
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

        public static ICube FxVega(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
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
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

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
                                { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
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

        public static ICube AssetIrDelta(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy = null)
        {
            var bumpSize = 0.0001;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
                { "AssetId", typeof(string) },
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
                    .Concat(portfolio.Instruments.Where(x => (x is FxForward fx) && (model.FundingModel.FxMatrix.DiscountCurveMap[fx.DomesticCCY] == curve.Key || model.FundingModel.FxMatrix.DiscountCurveMap[fx.ForeignCCY] == curve.Key || fx.ForeignDiscountCurve == curve.Key)))
                    .ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;

                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var pvCube = subPortfolio.PV(model, reportingCcy ?? curveObj.Currency);
                var pvRows = pvCube.GetAllRows();

                var tidIx = pvCube.GetColumnIndex("TradeId");
                var tTypeIx = pvCube.GetColumnIndex("TradeType");

                var bumpedCurves = curveObj.BumpScenarios(bumpSize, lastDateInBook);

                var results = new List<Tuple<Dictionary<string, object>, double>>[bumpedCurves.Count];
                var bcList = bumpedCurves.ToList();
                ParallelUtils.Instance.For(0, results.Length, 1, ii =>
                {
                    results[ii] = new List<Tuple<Dictionary<string, object>, double>>();
                    var bCurve = bcList[ii];

                    var newModel = model.Clone();
                    newModel.FundingModel.Curves[curve.Key] = bCurve.Value;
                    var bumpedPVCube = subPortfolio.PV(newModel, reportingCcy ?? curveObj.Currency);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        var delta = bumpedRows[i].Value - pvRows[i].Value;

                        if (delta != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "TradeType", bumpedRows[i].MetaData[tTypeIx] },
                                { "AssetId", curve.Key },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "IrDelta" }
                            };
                            results[ii].Add(new Tuple<Dictionary<string, object>, double>(row, delta));
                        }
                    }
                }).Wait();


                for (var i = 0; i < results.Length; i++)
                    for (var j = 0; j < results[i].Count(); j++)
                    {
                        if (results[i][j].Item1 != null)
                            cube.AddRow(results[i][j].Item1, results[i][j].Item2);
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
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", bumpedRows[i].MetaData[tidIx] },
                        { "Metric", "CorrelDelta" }
                    };
                    cube.AddRow(row, cDelta);
                }
            }

            return cube;
        }

        public static ICube AssetTheta(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var pvCube = portfolio.PV(model, reportingCcy);
            var cashCube = portfolio.FlowsT0(model, reportingCcy);
            var pvRows = pvCube.GetAllRows();
            var cashRows = cashCube.GetAllRows();
            var tidIx = pvCube.GetColumnIndex("TradeId");
            var tTypeIx = pvCube.GetColumnIndex("TradeType");

            var rolledModel = model.RollModel(fwdValDate, currencyProvider);

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
                        { "TradeType", pvRowsFwd[i].MetaData[tTypeIx] },
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
                        { "TradeType", cashRows[i].MetaData[tTypeIx] },
                        { "Metric", "CashMove" }
                    };
                    cube.AddRow(row, cash);
                }
            }

            return cube;
        }

        public static ICube AssetThetaCharm(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
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
            var tTypeIx = pvCube.GetColumnIndex("TradeType");

            var rolledModel = model.RollModel(fwdValDate, currencyProvider);

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
                        { "TradeType", pvRowsFwd[i].MetaData[tTypeIx] },
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
                        { "TradeType", cashRows[i].MetaData[tTypeIx] },
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
            var ttId = charmCube.GetColumnIndex("TradeType");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "TradeType", charmRow.MetaData[ttId] },
                    { "AssetId", charmRow.MetaData[aId] },
                    { "PointLabel", charmRow.MetaData[plId] },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            //charm-fx
            baseDeltaCube = FxDelta(portfolio, model, reportingCcy, currencyProvider);
            rolledDeltaCube = FxDelta(portfolio, rolledModel, reportingCcy, currencyProvider);
            charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var fId = charmCube.GetColumnIndex("AssetId");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "AssetId", charmRow.MetaData[fId] },
                    { "TradeType", charmRow.MetaData[ttId] },
                    { "PointLabel", string.Empty },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            return cube;
        }

        public static ICube AssetGreeks(this Portfolio portfolio, IAssetFxModel model, DateTime fwdValDate, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            ICube cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
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
            var tTypeIx = pvCube.GetColumnIndex("TradeType");

            var rolledModel = model.RollModel(fwdValDate, currencyProvider);

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

            //setup to run in parallel
            var tasks = new Dictionary<string, Task<ICube>>
            {
                { "AssetDeltaGamma", new Task<ICube>(() => AssetDeltaGamma(portfolio, model)) },
                { "AssetVega", new Task<ICube>(() => AssetVega(portfolio, model, reportingCcy)) },
                { "FxVega", new Task<ICube>(() => FxVega(portfolio, model, reportingCcy)) },
                { "RolledDeltaGamma", new Task<ICube>(() => AssetDeltaGamma(portfolio, rolledModel)) },
                { "FxDeltaGamma", new Task<ICube>(() => FxDelta(portfolio, model, reportingCcy, currencyProvider, true)) },
                { "RolledFxDeltaGamma", new Task<ICube>(() => FxDelta(portfolio, rolledModel, reportingCcy, currencyProvider, true)) },
                { "IrDelta", new Task<ICube>(() => AssetIrDelta(portfolio, model, reportingCcy)) }
            };

            ParallelUtils.Instance.QueueAndRunTasks(tasks.Values);


            //delta
            //var baseDeltaGammaCube = AssetDeltaGamma(portfolio, model);
            var baseDeltaGammaCube = tasks["AssetDeltaGamma"].Result;
            cube = cube.Merge(baseDeltaGammaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //vega
            //var assetVegaCube = AssetVega(portfolio, model, reportingCcy);
            var assetVegaCube = tasks["AssetVega"].Result;
            cube = cube.Merge(assetVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });
            //var fxVegaCube = FxVega(portfolio, model, reportingCcy);
            var fxVegaCube = tasks["FxVega"].Result;
            cube = cube.Merge(fxVegaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            });

            //charm-asset
            //var rolledDeltaGammaCube = AssetDeltaGamma(portfolio, rolledModel);
            var rolledDeltaGammaCube = tasks["RolledDeltaGamma"].Result;

            var baseDeltaCube = baseDeltaGammaCube.Filter(new Dictionary<string, object> { { "Metric", "Delta" } });
            var rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { "Metric", "Delta" } });
            var rolledGammaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { "Metric", "Gamma" } });
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
            cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
            {
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { "Metric", "AssetGammaT1" },
            });

            //charm-fx
            //baseDeltaCube = FxDelta(portfolio, model, reportingCcy, currencyProvider, true);
            baseDeltaCube = tasks["FxDeltaGamma"].Result;
            cube = cube.Merge(baseDeltaCube, new Dictionary<string, object>
            {
                 { "PointLabel", string.Empty },
                 { "Currency", string.Empty },
            });
            //rolledDeltaGammaCube = FxDelta(portfolio, rolledModel, reportingCcy, currencyProvider,true);
            rolledDeltaGammaCube = tasks["RolledFxDeltaGamma"].Result;
            rolledDeltaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { "Metric", "FxSpotDelta" } });
            rolledGammaCube = rolledDeltaGammaCube.Filter(new Dictionary<string, object> { { "Metric", "FxSpotGamma" } });

            cube = cube.Merge(rolledDeltaCube, new Dictionary<string, object>
            {
                 { "PointLabel", string.Empty },
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { "Metric", "FxSpotDeltaT1" },
            });

            cube = cube.Merge(rolledGammaCube, new Dictionary<string, object>
            {
                 { "PointLabel", string.Empty },
                 { "Currency", string.Empty },
            }, new Dictionary<string, object>
            {
                 { "Metric", "FxSpotGammaT1" },
            });

            charmCube = rolledDeltaCube.Difference(baseDeltaCube);
            var fId = charmCube.GetColumnIndex("AssetId");
            foreach (var charmRow in charmCube.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", charmRow.MetaData[tidIx] },
                    { "TradeType", charmRow.MetaData[tTypeIx] },
                    { "AssetId", charmRow.MetaData[fId] },
                    { "PointLabel", string.Empty },
                    { "Currency", string.Empty },
                    { "Metric", "Charm" }
                };
                cube.AddRow(row, charmRow.Value);
            }

            //ir-delta

            var baseIrDeltacube = tasks["IrDelta"].Result;
            cube = cube.Merge(baseIrDeltacube, new Dictionary<string, object>
            {
                 { "Currency", reportingCcy.Ccy },
            });

            return cube;
        }

        public static IAssetFxModel RollModel(this IAssetFxModel model, DateTime fwdValDate, ICurrencyProvider currencyProvider)
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
