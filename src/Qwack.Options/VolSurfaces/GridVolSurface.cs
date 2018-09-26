using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Options.VolSurfaces
{
    /// <summary>
    /// A volatility surface which returns a value interpolated from a grid of vols for varying strikes and maturities
    /// Strikes can be either absolute or delta type
    /// Interpolation method for strike and time dimensions can be specified seperately
    /// </summary>
    public class GridVolSurface : IVolSurface
    {
        public string Name { get; set; }
        public DateTime OriginDate { get; set; }
        public double[] Strikes { get; set; }
        public StrikeType StrikeType { get; set; }
        public Interpolator1DType StrikeInterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;
        public double[] ExpiriesDouble { get; set; }
        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearInVariance;
        public double[][] Volatilities { get; set; }
        public DateTime[] Expiries { get; set; }
        public string[] PillarLabels { get; set; }
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;

        public Currency Currency { get; set; }
        public string AssetId { get; set; }
        public IInterpolator2D LocalVolGrid { get; set; }

        private IInterpolator1D[] _interpolators;

        public GridVolSurface()         {        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols, string[] pillarLabels = null)
        {
            if (pillarLabels == null)
                PillarLabels = expiries.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                PillarLabels = pillarLabels;

            Build(originDate, strikes, expiries, vols);
        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols, 
            StrikeType strikeType, Interpolator1DType strikeInterpType, Interpolator1DType timeInterpType, 
            DayCountBasis timeBasis, string[] pillarLabels = null)
        {
            StrikeType = strikeType;
            StrikeInterpolatorType = strikeInterpType;
            TimeInterpolatorType = timeInterpType;
            TimeBasis = timeBasis;

            if (pillarLabels == null)
                PillarLabels = expiries.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                PillarLabels = pillarLabels;

            Build(originDate, strikes, expiries, vols);
        }

        public void Build(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols)
        {
            OriginDate = originDate;
            Strikes = strikes;
            Expiries = expiries;
            Volatilities = vols;
            ExpiriesDouble = Expiries.Select(t => TimeBasis.CalculateYearFraction(originDate, t)).ToArray();
            _interpolators = vols.Select((v, ix) => 
                InterpolatorFactory.GetInterpolator(Strikes, v, StrikeInterpolatorType)).ToArray();
        }

        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward)
        {
            if (StrikeType == StrikeType.Absolute)
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble, 
                    _interpolators.Select(x => x.Interpolate(strike)).ToArray(), 
                    TimeInterpolatorType);
                return interpForStrike.Interpolate(maturity);
            }
            else
            {
                var fwd = forward;
                var cp = strike < 0 ? OptionType.Put : OptionType.Call;
                Func<double,double> testFunc = (deltaK =>
                {
                    var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(-deltaK)).ToArray(),
                   TimeInterpolatorType);
                    var vol = interpForStrike.Interpolate(maturity);
                    var absK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, maturity, vol);
                    return absK - strike;
                });

                var solvedStrike = -Qwack.Math.Solvers.Brent.BrentsMethodSolve(testFunc, -0.999999999, -0.000000001, 1e-8);
                var interpForSolvedStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(solvedStrike)).ToArray(),
                   TimeInterpolatorType);
                return interpForSolvedStrike.Interpolate(maturity);
            }
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward)
        {
            if (deltaStrike > 1.0 || deltaStrike < -1.0)
                throw new ArgumentOutOfRangeException($"Delta strike must be in range -1.0 < x < 1.0 - value was {deltaStrike}");

            if (StrikeType == StrikeType.ForwardDelta)
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                    _interpolators.Select(x => x.Interpolate(deltaStrike)).ToArray(),
                    TimeInterpolatorType);
                return interpForStrike.Interpolate(maturity);
            }
            else
            {
                var fwd = forward;
                var cp = deltaStrike < 0 ? OptionType.Put : OptionType.Call;
                Func<double, double> testFunc = (absK =>
                {
                    var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(absK)).ToArray(),
                   TimeInterpolatorType);
                    var vol = interpForStrike.Interpolate(maturity);
                    var deltaK = BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol, cp);
                    return deltaK - System.Math.Abs(deltaStrike);
                });

                var solvedStrike = Qwack.Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 10*fwd, 1e-8);
                var interpForSolvedStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(solvedStrike)).ToArray(),
                   TimeInterpolatorType);
                return interpForSolvedStrike.Interpolate(maturity);
            }
        }

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => GetVolForDeltaStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastBumpIx = Expiries.Length;

            if (LastSensitivityDate.HasValue)
            {
                var ix = Array.BinarySearch(Expiries, LastSensitivityDate.Value);
                ix = (ix < 0) ? ~ix : ix;
                ix += 2;
                lastBumpIx = System.Math.Min(ix, lastBumpIx); //cap at last pillar
            }

            for (var i=0;i< lastBumpIx; i++)
            {
                var volsBumped = (double[][])Volatilities.Clone();
                volsBumped[i] = volsBumped[i].Select(x => x + bumpSize).ToArray();
                o.Add(PillarLabels[i],
                    new GridVolSurface(OriginDate, Strikes, Expiries, volsBumped, StrikeType, StrikeInterpolatorType, TimeInterpolatorType, TimeBasis, PillarLabels)
                    {
                        Currency = Currency,
                        AssetId = AssetId
                    });
            }

            return o;
        }

        public DateTime PillarDatesForLabel(string label)
        {
            var labelIx = Array.IndexOf(PillarLabels, label);
            return Expiries[labelIx];
        }
    }
}
