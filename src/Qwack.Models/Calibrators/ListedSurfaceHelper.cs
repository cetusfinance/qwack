using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Options.VolSurfaces;
using Qwack.Options;
using Qwack.Transport.BasicTypes;
using Qwack.Core.Basic;

namespace Qwack.Models.Calibrators
{
    public static class ListedSurfaceHelper
    {
        public static void ImplyVols(List<ListedOptionSettlementRecord> optionSettlements, Dictionary<string, double> futuresPrices, IIrCurve discountCurve = null)
        {
            var o = new Dictionary<DateTime, IInterpolator1D>();

            foreach (var s in optionSettlements)
            {
                double fut;
                if (s.UnderlyingFuturesPrice != 0)
                    fut = s.UnderlyingFuturesPrice;
                else if (!futuresPrices.TryGetValue(s.UnderlyingFuturesCode, out fut))
                {
                    throw new Exception($"No future price found for contract {s}");
                }

                var t = s.ValDate.CalculateYearFraction(s.ExpiryDate, DayCountBasis.ACT365F);
                var r = 0.0;

                if (s.MarginType == OptionMarginingType.Regular)
                {
                    if (discountCurve == null)
                        throw new Exception("To strip vols from options with regular margining, a discount curve must be supplied");
                    r = discountCurve.GetRate(s.ExpiryDate);
                }

                if (s.ExerciseType == OptionExerciseType.European || s.MarginType == OptionMarginingType.FuturesStyle)
                    s.ImpliedVol = BlackFunctions.BlackImpliedVol(fut, s.Strike, r, t, s.PV, s.CallPut);
                else if (s.ExerciseType == OptionExerciseType.American)
                    s.ImpliedVol = BinomialTree.AmericanFuturesOptionImpliedVol(fut, s.Strike, r, t, s.PV, s.CallPut);
                else
                    throw new Exception("Unable to handle option in stripper");
            }
        }

        public static Dictionary<DateTime, IInterpolator1D> ToDeltaSmiles(this List<ListedOptionSettlementRecord> optionSettlements, Dictionary<string, double> futuresPrices, SmileOrderOfPrecedence orderOfP=SmileOrderOfPrecedence.UseOTM)
        {
            var output = new Dictionary<DateTime, IInterpolator1D>();

            var byExpiry = optionSettlements.GroupBy(r => r.ExpiryDate).OrderBy(o => o.Key);
            foreach(var expGroup in byExpiry)
            {
                var expiry = expGroup.Key;
                if (expGroup.Select(x => x.UnderlyingFuturesCode).Distinct().Count() != 1)
                    throw new Exception($"Inconsistent underlying contracts for expiry {expiry}");

                if (expGroup.Select(x => x.ValDate).Distinct().Count() != 1)
                    throw new Exception($"Inconsistent value dates for expiry {expiry}");

                var t = expGroup.First().ValDate.CalculateYearFraction(expiry, DayCountBasis.ACT365F);

                double fut;
                if (expGroup.First().UnderlyingFuturesPrice != 0)
                    fut = expGroup.First().UnderlyingFuturesPrice;
                else if (!futuresPrices.TryGetValue(expGroup.First().UnderlyingFuturesCode, out fut))
                {
                    throw new Exception($"No future price found for contract {expGroup.First().UnderlyingFuturesCode}");
                }


                var filtered = new List<SmilePoint>();
                var byStrike = expGroup.GroupBy(e => e.Strike).OrderBy(o => o.Key);
                foreach (var strikeGrp in byStrike)
                {
                    var k = strikeGrp.Key;
                    if (strikeGrp.Count() > 2)
                        throw new Exception($"Did not expect more than two options at strike {k}");
                    switch (orderOfP)
                    {
                        case SmileOrderOfPrecedence.Average:
                            {
                                var v = strikeGrp.Average(s => s.ImpliedVol);
                                filtered.Add(
                                    new SmilePoint
                                    {
                                        Strike = -BlackFunctions.BlackDelta(fut,k,0.0,t,v,OptionType.P),
                                        ImpliedVol = v
                                    });
                                break;
                            }
                        case SmileOrderOfPrecedence.UseOTM:
                            {
                                ListedOptionSettlementRecord r;
                                if (fut > k)
                                    r = strikeGrp.Where(sg => sg.CallPut == OptionType.P).SingleOrDefault();
                                else
                                    r = strikeGrp.Where(sg => sg.CallPut == OptionType.C).SingleOrDefault();

                                if (r != null && r.ImpliedVol>1e-8)
                                    filtered.Add(
                                        new SmilePoint
                                        {
                                            Strike = -BlackFunctions.BlackDelta(fut, k, 0.0, t, r.ImpliedVol, OptionType.P),
                                            ImpliedVol = r.ImpliedVol
                                        });
                                break;
                            }
                    }
                }

                //now we have filtered to a single vol per-strike...
                if (filtered.Any())
                    output[expiry] = InterpolatorFactory.GetInterpolator(filtered.Select(f => f.Strike).ToArray(), filtered.Select(f => f.ImpliedVol).ToArray(), Interpolator1DType.CubicSpline);
            }
            return output;
        }

        public static RiskyFlySurface ToRiskyFlySurface(this Dictionary<DateTime, IInterpolator1D> smiles, DateTime valDate, double[] fwds, ICurrencyProvider currencyProvider)
        {
            var wingDeltas = new[] { 0.25, 0.1 };
            var expiries = smiles.Keys.ToArray();
            var atmVols = smiles.Select(x => x.Value.Interpolate(0.5)).ToArray();
            var riskies = smiles.Select(x =>
                    wingDeltas.Select(w => x.Value.Interpolate(1.0 - w) - x.Value.Interpolate(w))
                    .ToArray())
                .ToArray();
            var flies = smiles.Select((x,ix) =>
                    wingDeltas.Select(w => (x.Value.Interpolate(1.0 - w) + x.Value.Interpolate(w))/2.0-atmVols[ix])
                    .ToArray())
                .ToArray();

            SmoothSmiles(wingDeltas, smiles, ref riskies, ref flies);

            var o = new RiskyFlySurface(valDate, atmVols, expiries, wingDeltas, riskies, flies, fwds, WingQuoteType.Arithmatic,
                AtmVolType.ZeroDeltaStraddle, Interpolator1DType.CubicSpline, Interpolator1DType.LinearInVariance)
            {
                Currency = currencyProvider.GetCurrency("USD")
            };
            return o;
        }

        public static RiskyFlySurface ToRiskyFlySurfaceStepFlat(this Dictionary<DateTime, IInterpolator1D> smiles, DateTime valDate, IPriceCurve priceCurve, List<DateTime> allExpiries, ICurrencyProvider currencyProvider)
        {
            var wingDeltas = new[] { 0.25, 0.1 };
            var expiries = smiles.Keys.ToArray();
            var expiriesDouble = expiries.Select(e=>e.ToOADate()).ToArray();
            var atmVols = smiles.Select(x => x.Value.Interpolate(0.5)).ToArray();
            var riskies = smiles.Select(x =>
                    wingDeltas.Select(w => x.Value.Interpolate(1.0 - w) - x.Value.Interpolate(w))
                    .ToArray())
                .ToArray();
            var flies = smiles.Select((x, ix) =>
                    wingDeltas.Select(w => (x.Value.Interpolate(1.0 - w) + x.Value.Interpolate(w)) / 2.0 - atmVols[ix])
                    .ToArray())
                .ToArray();

            SmoothSmiles(wingDeltas, smiles, ref riskies, ref flies);

            var fwds = allExpiries.Select(x => priceCurve.GetPriceForDate(x)).ToArray();

            var atmInterp = InterpolatorFactory.GetInterpolator(expiriesDouble, atmVols, Interpolator1DType.LinearInVariance);
            var riskyInterps = wingDeltas.Select((w, ixw) => InterpolatorFactory.GetInterpolator(expiriesDouble, riskies.Select(r => r[ixw]).ToArray(), Interpolator1DType.LinearFlatExtrap)).ToArray();
            var flyInterps = wingDeltas.Select((w, ixw) => InterpolatorFactory.GetInterpolator(expiriesDouble, flies.Select(f => f[ixw]).ToArray(), Interpolator1DType.LinearFlatExtrap)).ToArray();

            var expandedRiskies = allExpiries.Select(e => wingDeltas.Select((w, ixw) => riskyInterps[ixw].Interpolate(e.ToOADate())).ToArray()).ToArray();
            var expandedFlies = allExpiries.Select(e => wingDeltas.Select((w, ixw) => flyInterps[ixw].Interpolate(e.ToOADate())).ToArray()).ToArray();
            var expandedAtms = allExpiries.Select(e => atmInterp.Interpolate(e.ToOADate())).ToArray();
            var expandedFwds = allExpiries.Select(e => priceCurve.GetPriceForDate(e)).ToArray();
            var o = new RiskyFlySurface(valDate, expandedAtms, allExpiries.ToArray(), wingDeltas, expandedRiskies, expandedFlies, expandedFwds, WingQuoteType.Arithmatic,
                AtmVolType.ZeroDeltaStraddle, Interpolator1DType.CubicSpline, Interpolator1DType.NextValue)
            {
                Currency = currencyProvider.GetCurrency("USD")
            };
            return o;
        }

        public static void SmoothSmiles(double[] wingDeltas, Dictionary<DateTime, IInterpolator1D> smiles, ref double[][] riskies, ref double[][] flies)
        {
            var expiries = smiles.Keys.OrderBy(x => x).ToArray();
            for (var w = 0; w < wingDeltas.Length; w++)
            {
                var goodSmilesAhead = true;
                for (var i = 1; i < expiries.Length; i++) //assume first smile is complete
                {
                    var smile = smiles[expiries[i]];
                    if (smile.MinX > wingDeltas[w] || smile.MaxX < (1 - wingDeltas[w]))
                    {
                        if (!goodSmilesAhead)
                        {
                            riskies[i][w] = riskies[i - 1][w];
                            flies[i][w] = flies[i - 1][w];
                        }
                        else
                        {
                            var t = 1;
                            while (i + t < riskies.Length)
                            {
                                if (smile.MinX <= wingDeltas[w] || smile.MaxX >= (1 - wingDeltas[w]))
                                {
                                    var slopeRR = (riskies[i + t][w] - riskies[i - 1][w]) / (1.0 + t);
                                    riskies[i][w] = riskies[i - 1][w] + slopeRR;
                                    var slopeBF = (flies[i + t][w] - flies[i - 1][w]) / (1.0 + t);
                                    flies[i][w] = flies[i - 1][w] + slopeBF;
                                    break;
                                }
                                t++;
                            }
                            if (i + t >= riskies.Length)
                            {
                                goodSmilesAhead = false;
                                //riskies[i][w] = riskies[i - 1][w];
                                //flies[i][w] = flies[i - 1][w];
                            }
                        }
                    }
                }
            }
        }

        public static RiskyFlySurface ToATMSurface(this Dictionary<DateTime, IInterpolator1D> smiles, DateTime valDate, double[] fwds)
        {
            var wingDeltas = new[] { 0.25 };
            var expiries = smiles.Keys.ToArray();
            var atmVols = smiles.Select(x => x.Value.Interpolate(0.5)).ToArray();
            var riskies = smiles.Select(x => new[] { 0.0 }).ToArray();
            var flies = smiles.Select(x => new[] { 0.0 }).ToArray();

            var o = new RiskyFlySurface(valDate, atmVols, expiries, wingDeltas, riskies, flies, fwds, WingQuoteType.Arithmatic, AtmVolType.ZeroDeltaStraddle, Interpolator1DType.Linear, Interpolator1DType.LinearInVariance);
            return o;
        }
    }

    public class ListedOptionSettlementRecord
    {
        public OptionType CallPut { get; set; }
        public double Strike { get; set; }
        public double PV { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime ValDate { get; set; }
        public OptionExerciseType ExerciseType { get; set; }
        public OptionMarginingType MarginType { get; set; }
        public string UnderlyingFuturesCode { get; set; }
        public double UnderlyingFuturesPrice{ get; set; }
        public double ImpliedVol { get; set; }
    }

    public class SmilePoint
    {
        public double Strike { get; set; }
        public double ImpliedVol { get; set; }
    }

    public enum SmileOrderOfPrecedence
    {
        UseOTM,
        Average
    }
}
