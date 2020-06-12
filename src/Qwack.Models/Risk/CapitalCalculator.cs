using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Utils.Parallel;
using static System.Math;

namespace Qwack.Models.Risk
{
    public class CapitalCalculator
    {
        public static double PVCapital_BII_IMM(DateTime originDate, DateTime[] EADDates, double[] EADs, HazzardCurve hazzardCurve, 
            IIrCurve discountCurve, double LGD, Portfolio portfolio)
        {
            if (EADDates.Length != EADs.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");
            
            var pd = hazzardCurve.ConstantPD;
            var epees = EADs.Select((d,ix) => EADs.Skip(ix).Max()).ToArray();
            var Ms = EADDates.Select(d => Max(1.0, portfolio.WeightedMaturity(d))).ToArray();
            var ks = epees.Select((e, ix) => BaselHelper.K(pd, LGD, Ms[ix]) * e *1.4).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);
            return pvCapital;
        }

        public static double PvCcrCapital_BII_SM(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, Portfolio portfolio, 
            HazzardCurve hazzardCurve, Currency reportingCurrency, IIrCurve discountCurve, double LGD, Dictionary<string, string> assetIdToHedgeMap, 
            Dictionary<string, double> hedgeGroupCCFs, ICurrencyProvider currencyProvider, double[] epeProfile, double[] eadProfile = null)
        {
            var pd = hazzardCurve.ConstantPD;
            var eads = eadProfile ?? EAD_BII_SM(originDate, EADDates, epeProfile, models, portfolio, reportingCurrency, assetIdToHedgeMap, hedgeGroupCCFs, currencyProvider);

            var Ms = EADDates.Select(d => Max(1.0, portfolio.WeightedMaturity(d))).ToArray();
            var ks = eads.Select((e, ix) => BaselHelper.K(pd, LGD, Ms[ix]) * e).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);
            return pvCapital;
        }

        public static double PvCvaCapital_BII_SM(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, Portfolio portfolio, 
            Currency reportingCurrency, IIrCurve discountCurve, double partyWeight, Dictionary<string, string> assetIdToHedgeMap, 
            Dictionary<string, double> hedgeGroupCCFs, ICurrencyProvider currencyProvider, double[] epeProfile, double[] eadProfile=null)
        {
            var eads = eadProfile??EAD_BII_SM(originDate, EADDates, epeProfile, models, portfolio, reportingCurrency, assetIdToHedgeMap, hedgeGroupCCFs, currencyProvider);
            var Ms = EADDates.Select(d => Max(1.0,portfolio.WeightedMaturity(d))).ToArray();
            var dfs = Ms.Select(m => m == 0 ? 1.0 : (1.0 - Exp(-0.05 * m)) / (0.05 * m)).ToArray();
            var ks = eads.Select((e, ix) => XVACalculator.Capital_BaselII_CVA_SM(e * dfs[ix], Ms[ix], partyWeight)).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);

            return pvCapital;
        }

        public static (double CVA, double CCR) PvCapital_Split(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, 
            Portfolio portfolio, HazzardCurve hazzardCurve, Currency reportingCurrency, IIrCurve discountCurve, double LGD, double partyCVAWeight, double riskWeight, 
            Dictionary<string,string> assetIdToHedgeMap, Dictionary<string, double> hedgeGroupCCFs, ICurrencyProvider currencyProvider, double[] epeProfile,
            DateTime? B2B3ChangeDate=null, double[] eadProfile = null)
        {
            var pd = hazzardCurve.ConstantPD;
            if (!B2B3ChangeDate.HasValue)
                B2B3ChangeDate = DateTime.MaxValue;

            var eads = eadProfile ?? EAD_Split(originDate, EADDates, epeProfile, models, portfolio, reportingCurrency, assetIdToHedgeMap, hedgeGroupCCFs, B2B3ChangeDate.Value, currencyProvider);
            eads = eads.Select(e => e * riskWeight).ToArray();

            var Ms = EADDates.Select(d => Max(1.0,portfolio.WeightedMaturity(d))).ToArray();
            var dfs = Ms.Select(m => m == 0 ? 1.0 : (1.0 - Exp(-0.05 * m)) / (0.05 * m)).ToArray();

            var ksCCR = eads.Select((e, ix) => BaselHelper.K(pd, LGD, Ms[ix]) * e).ToArray();
            var ksCVA = eads.Select((e, ix) => XVACalculator.Capital_BaselII_CVA_SM(e * dfs[ix], Ms[ix], partyCVAWeight)).ToArray();

            var pvCapitalCCR = PvProfile(originDate, EADDates, ksCCR, discountCurve);
            var pvCapitalCVA = PvProfile(originDate, EADDates, ksCVA, discountCurve);

            return (pvCapitalCVA, pvCapitalCCR);
        }

        public static double[] EAD_Split(DateTime originDate, DateTime[] EADDates, double[] EPEs, IAssetFxModel[] models, Portfolio portfolio, Currency reportingCurrency, Dictionary<string, string> assetIdToHedgeMap, Dictionary<string, double> hedgeGroupCCFs, DateTime changeOverDate, ICurrencyProvider currencyProvider)
        {
            var changeOverIx = Array.BinarySearch(EADDates, changeOverDate);
            if (changeOverIx < 0)
                changeOverIx = ~changeOverIx;
            changeOverIx = Min(changeOverIx, EADDates.Length);

            var EADb2 = EAD_BII_SM(originDate, EADDates.Take(changeOverIx).ToArray(), EPEs.Take(changeOverIx).ToArray(), models, portfolio, reportingCurrency, assetIdToHedgeMap, hedgeGroupCCFs, currencyProvider);
            if (changeOverIx == EADDates.Length) //all under Basel II / SM
                return EADb2;

            var EADb3 = EAD_BIII_SACCR(originDate, EADDates.Skip(changeOverIx).ToArray(), EPEs.Skip(changeOverIx).ToArray(), models.Skip(changeOverIx).ToArray(), portfolio, reportingCurrency, assetIdToHedgeMap);

            return EADb2.Concat(EADb3).ToArray();
        }

        public static bool IsPrecious(string pair) => pair.StartsWith("X") || pair.Contains("/X");

        public static double[] EAD_BII_SM(DateTime originDate, DateTime[] EADDates, double[] EPEs, IAssetFxModel[] models, Portfolio portfolio, Currency reportingCurrency, Dictionary<string, string> assetIdToHedgeMap, Dictionary<string, double> hedgeGroupCCFs, ICurrencyProvider currencyProvider)
        {
            var beta = 1.4;
            if (!hedgeGroupCCFs.TryGetValue("FX", out var ccfFx))
                ccfFx = 0.025;

            var eads = new double[EADDates.Length];

            ParallelUtils.Instance.For(0, EADDates.Length, 1, i =>
            //for (var i = 0; i < EADDates.Length; i++)
            {
                if (EADDates[i] >= originDate)
                {
                    var m = models[i].Clone();
                    m.AttachPortfolio(portfolio);

                    var pv = EPEs[i];
                    var deltaSum = 0.0;
                    var assetDeltaCube = m.AssetCashDelta(reportingCurrency).Pivot("AssetId", AggregationAction.Sum);
                    foreach (var row in assetDeltaCube.GetAllRows())
                    {
                        var asset = (string)row.MetaData[0];
                        var hedgeSet = assetIdToHedgeMap[asset];
                        var ccf = hedgeGroupCCFs[hedgeSet];
                        var delta = Abs(row.Value); //need to consider buckets
                        deltaSum += ccf * delta;
                    }
                    var fxDeltaCube = m.FxDelta(reportingCurrency, currencyProvider, false, true).Pivot("AssetId", AggregationAction.Sum);
                    foreach (var row in fxDeltaCube.GetAllRows())
                    {
                        var pair = (string)row.MetaData[0];
                        var rate = m.FundingModel.GetFxRate(m.BuildDate, pair);
                        var ccfToUse = ccfFx;
                        if (IsPrecious(pair))
                        {
                            if (!assetIdToHedgeMap.TryGetValue(pair, out var hedgeSet))
                            {
                                var flippedPair = $"{pair.Substring(pair.Length - 3, 3)}/{pair.Substring(0, 3)}";
                                if (!assetIdToHedgeMap.TryGetValue(flippedPair, out hedgeSet))
                                {
                                    throw new Exception($"Could not find pair {pair} or its inverse in hedge set map");
                                }
                            }

                            ccfToUse = hedgeGroupCCFs[hedgeSet];
                        }
                        deltaSum += Abs(row.Value) / rate * ccfToUse;
                    }


                    eads[i] = Max(pv, deltaSum) * beta;
                }
            }).Wait();

            return eads;
        }

        public static double[] EAD_BIII_SACCR(DateTime originDate, DateTime[] EADDates, double[] EPEs, IAssetFxModel[] models, Portfolio portfolio, Currency reportingCurrency, Dictionary<string,string> assetIdToHedgeMap)
        {
            var eads = new double[EADDates.Length];
            for (var i = 0; i < EADDates.Length; i++)
            {
                if (EADDates[i] < originDate)
                    continue;
                var m = models[i].Clone();
                m.AttachPortfolio(portfolio);

                eads[i] = portfolio.SaCcrEAD(EPEs[i], m, reportingCurrency, assetIdToHedgeMap);
            }

            return eads;
        }

        public static double PVCapital_BII_IMM(DateTime originDate, ICube expectedEAD, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD, Portfolio portfolio)
        {
            (var eadDates, var eadValues) = XVACalculator.CubeToExposures(expectedEAD);
            return PVCapital_BII_IMM(originDate, eadDates, eadValues, hazzardCurve, discountCurve, LGD, portfolio);
        }

        public static double PvProfile(DateTime originDate, DateTime[] exposureDates, double[] exposures , IIrCurve discountCurve)
        {
            var capital = 0.0;
            var time = 0.0;

            if (exposureDates.Length != exposures.Length || exposures.Length < 1)
                throw new DataMisalignedException();

            if (exposureDates.Length == 1)
                return discountCurve.GetDf(originDate, exposureDates[0]) * exposures[0];

            for (var i = 0; i < exposureDates.Length - 1; i++)
            {
                var exposure = (exposures[i] + exposures[i + 1]) / 2.0;
                var df = discountCurve.GetDf(originDate, exposureDates[i+1]);
                var dt = exposureDates[i].CalculateYearFraction(exposureDates[i + 1], DayCountBasis.ACT365F);
                
                capital += exposure * dt * df;
                time += dt;
            }

            return capital / time;
        }

    }
}
