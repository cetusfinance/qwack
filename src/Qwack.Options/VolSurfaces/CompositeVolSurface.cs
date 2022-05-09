using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Transport.BasicTypes;

namespace Qwack.Options.VolSurfaces
{
    public class CompositeVolSurface : IVolSurface
    {
        public IVolSurface AssetSurface { get; }
        public IFundingModel FundingModel { get; }
        public double Correlation { get; }
        public CompositeVolType CalculationType { get; }
        public string Name { get; }
        public Currency Currency { get; set; }
        public string AssetId { get; set; }
        public Frequency OverrideSpotLag { get; set; }

        public CompositeVolSurface() => Pair = FundingModel?.FxMatrix?.GetFxPair(AssetSurface.Currency, Currency);
        public CompositeVolSurface(string name, string assetId, Currency ccy, IVolSurface assetSurface, IFundingModel fundingModel, double correlation, CompositeVolType calculationType)
        {
            AssetSurface = assetSurface;
            FundingModel = fundingModel;
            Correlation = correlation;
            CalculationType = calculationType;
            Name = name;
            AssetId = assetId;
            Currency = ccy;

            Pair = FundingModel.FxMatrix.GetFxPair(AssetSurface.Currency, Currency);
        }

        public DateTime OriginDate => AssetSurface.OriginDate;
        public DateTime[] Expiries => AssetSurface.Expiries;
        public DateTime PillarDatesForLabel(string label) => AssetSurface.PillarDatesForLabel(label);

        public IInterpolator2D LocalVolGrid { get; set; }

        private FxPair Pair { get; }


        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var aDict = AssetSurface.GetATMVegaScenarios(bumpSize, LastSensitivityDate);
            var oDict = aDict.ToDictionary(kv => kv.Key, kv => (IVolSurface)new CompositeVolSurface(Name, AssetId, Currency, kv.Value, FundingModel, Correlation, CalculationType));

            var fDict = FundingModel.GetVolSurface(Pair.ToString()).GetATMVegaScenarios(bumpSize, LastSensitivityDate);
            foreach (var kv in fDict)
            {
                var fModel = FundingModel.DeepClone(null);
                fModel.VolSurfaces[Pair.ToString()] = kv.Value;
                oDict.Add(kv.Key, new CompositeVolSurface(Name, AssetId, Currency, AssetSurface, fModel, Correlation, CalculationType));
            }

            return oDict;
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), forward);

        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward)
        {
            var expiry = OriginDate.AddYearFraction(maturity, DayCountBasis.Act365F);
            var pair = FundingModel.FxMatrix.GetFxPair(AssetSurface.Currency, Currency);
            var fxFwd = FundingModel.GetFxRate(pair.SpotDate(expiry), pair.Domestic, pair.Foreign);

            double vA, vF;
            switch (CalculationType)
            {
                case CompositeVolType.Black:
                    vA = AssetSurface.GetVolForAbsoluteStrike(forward / fxFwd, expiry, forward / fxFwd);
                    vF = FundingModel.GetVolSurface(pair.ToString()).GetVolForAbsoluteStrike(fxFwd, expiry, fxFwd);
                    break;
                case CompositeVolType.AssetSkewOnly:
                    vA = AssetSurface.GetVolForAbsoluteStrike(strike / fxFwd, expiry, forward / fxFwd);
                    vF = FundingModel.GetVolSurface(pair.ToString()).GetVolForAbsoluteStrike(fxFwd, expiry, fxFwd);
                    break;
                default:
                    throw new Exception($"Unable to handle calc type {CalculationType}");
            }

            var vC = System.Math.Sqrt(vA * vA + vF * vF + 2.0 * Correlation * vA * vF);
            return vC;
        }

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => GetVolForDeltaStrike(strike, OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), forward);

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward)
        {
            var cp = deltaStrike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (absK =>
            {
                var volTest = GetVolForAbsoluteStrike(absK, maturity, forward);
                var deltaK = BlackFunctions.BlackDelta(forward, absK, 0, maturity, volTest, cp);
                return deltaK - System.Math.Abs(deltaStrike);
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 10 * forward, 1e-8);
            return GetVolForAbsoluteStrike(solvedStrike, maturity, forward);
        }

        public double InverseCDF(DateTime expiry, double fwd, double p) => VolSurfaceEx.InverseCDFex(this, OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), fwd, p);
        public double CDF(DateTime expiry, double fwd, double strike) => this.GenerateCDF2(100, expiry, fwd).Interpolate(strike);
    }
}
