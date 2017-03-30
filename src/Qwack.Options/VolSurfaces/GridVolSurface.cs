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
        public DateTime OriginDate { get; set; }
        public double[] Strikes { get; set; }
        public StrikeType StrikeType { get; set; }
        public Interpolator1DType StrikeInterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;
        public double[] ExpiriesDouble { get; set; }
        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearInVariance;
        public double[][] Volatilities { get; set; }
        public DateTime[] Expiries { get; set; }
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;
        public Func<double,double> ForwardCurve { get; set; }
        private IInterpolator1D[] _interpolators;

        public GridVolSurface()         {        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols)
        {
            Build(originDate, strikes, expiries, vols);
        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols, 
            StrikeType strikeType, Interpolator1DType strikeInterpType, Interpolator1DType timeInterpType, 
            DayCountBasis timeBasis)
        {
            StrikeType = strikeType;
            StrikeInterpolatorType = strikeInterpType;
            TimeInterpolatorType = timeInterpType;
            TimeBasis = timeBasis;

            Build(originDate, strikes, expiries, vols);
        }

        public void Build(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols)
        {
            OriginDate = originDate;
            Strikes = strikes;
            Expiries = expiries;
            ExpiriesDouble = Expiries.Select(t => TimeBasis.CalculateYearFraction(originDate, t)).ToArray();
            _interpolators = vols.Select((v, ix) => 
                InterpolatorFactory.GetInterpolator(Strikes, v, StrikeInterpolatorType)).ToArray();
        }

        public double GetVolForAbsoluteStrike(double strike, double maturity)
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
                var fwd = ForwardCurve(maturity);
                var cp = strike < 0 ? OptionType.Put : OptionType.Call;
                Func<double,double> testFunc = (deltaK =>
                {
                    var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(deltaK)).ToArray(),
                   TimeInterpolatorType);
                    var vol = interpForStrike.Interpolate(maturity);
                    var absK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, maturity, vol);
                    return absK - strike;
                });

                var solvedStrike = Qwack.Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 0.999999999, 1e-8);
                var interpForSolvedStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(solvedStrike)).ToArray(),
                   TimeInterpolatorType);
                return interpForSolvedStrike.Interpolate(maturity);
            }
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry)
        {
            return GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry));
        }

        public double GetVolForDeltaStrike(double deltaStrike, double maturity)
        {
            if (StrikeType == StrikeType.ForwardDelta)
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                    _interpolators.Select(x => x.Interpolate(deltaStrike)).ToArray(),
                    TimeInterpolatorType);
                return interpForStrike.Interpolate(maturity);
            }
            else
            {
                var fwd = ForwardCurve(maturity);
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

        public double GetVolForDeltaStrike(double strike, DateTime expiry)
        {
            return GetVolForDeltaStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry));
        }

        public double GetFwdATMVol(DateTime startDate, DateTime endDate)
        {
            double t1 = TimeBasis.CalculateYearFraction(OriginDate, startDate);
            double t2 = TimeBasis.CalculateYearFraction(OriginDate, endDate);
            double tt = t2 - t1;

            double vol1 = GetVolForAbsoluteStrike(ForwardCurve(t1), startDate);
            double vol2 = GetVolForAbsoluteStrike(ForwardCurve(t2), endDate);

            double var1 = vol1 * vol1 * t1;
            double var2 = vol2 * vol2 * t2;

            double var = var2 - var1;
            double vol = System.Math.Sqrt(var / tt);
            return vol;
        }

    }
}
