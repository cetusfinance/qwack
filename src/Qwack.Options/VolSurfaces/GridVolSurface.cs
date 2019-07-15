using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using static Qwack.Math.Statistics;
using System;
using System.Collections.Concurrent;
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
    public class GridVolSurface : IVolSurface, IATMVolSurface
    {
        private readonly bool _allowCaching = true;
        private ConcurrentDictionary<string, double> _absVolCache = new ConcurrentDictionary<string, double>();
        private ConcurrentDictionary<string, double> _deltaVolCache = new ConcurrentDictionary<string, double>();

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

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols, 
            StrikeType strikeType, Interpolator1DType strikeInterpType, Interpolator1DType timeInterpType, 
            DayCountBasis timeBasis, string[] pillarLabels = null):base()
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
            var key = $"{strike:f6}~{maturity:f3}~{forward:f6}";
            if (_allowCaching && _absVolCache.TryGetValue(key, out var vol))
                return vol;

            if (StrikeType == StrikeType.Absolute)
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                    _interpolators.Select(x => x.Interpolate(strike)).ToArray(),
                    TimeInterpolatorType);
                vol = interpForStrike.Interpolate(maturity);
            }
            else
            {
                var fwd = forward;
                var cp = strike < 0 ? OptionType.Put : OptionType.Call;
                Func<double, double> testFunc = (deltaK =>
                {
                    var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(-deltaK)).ToArray(),
                   TimeInterpolatorType);
                    var vol2 = interpForStrike.Interpolate(maturity);
                    var absK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, maturity, vol2);
                    return absK - strike;
                });

                var solvedStrike = -Math.Solvers.Brent.BrentsMethodSolve(testFunc, -0.999999999, -0.000000001, 1e-8);
                if (solvedStrike == 0.000000001 || solvedStrike == 0.999999999) //out of bounds
                {
                    var upperK = testFunc(-0.000000001);
                    var lowerK = testFunc(-0.999999999);
                    if (System.Math.Abs(upperK - fwd) < System.Math.Abs(lowerK - fwd))
                        solvedStrike = 0.000000001;
                    else
                        solvedStrike = 0.999999999;
                }
                var interpForSolvedStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(solvedStrike)).ToArray(),
                   TimeInterpolatorType);
                vol = interpForSolvedStrike.Interpolate(maturity);
            }

            if (_allowCaching) _absVolCache[key] = vol;
            return vol;
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public double RiskReversal(double deltaStrike, double maturity, double forward)
        {
            var callVol = GetVolForDeltaStrike(deltaStrike, maturity, forward);
            var putVol = GetVolForDeltaStrike(-deltaStrike, maturity, forward);

            return callVol - putVol;
        }

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward)
        {
            if (deltaStrike > 1.0 || deltaStrike < -1.0)
                throw new ArgumentOutOfRangeException($"Delta strike must be in range -1.0 < x < 1.0 - value was {deltaStrike}");

            var key = $"{deltaStrike:f6}~{maturity:f3}~{forward:f6}";
            if (_allowCaching && _deltaVolCache.TryGetValue(key, out var vol))
                return vol;

            if (StrikeType == StrikeType.ForwardDelta)
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                    _interpolators.Select(x => x.Interpolate(deltaStrike)).ToArray(),
                    TimeInterpolatorType);
                vol = interpForStrike.Interpolate(maturity);
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
                    var vol2 = interpForStrike.Interpolate(maturity);
                    var deltaK = BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol2, cp);
                    return deltaK - System.Math.Abs(deltaStrike);
                });

                var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 10 * fwd, 1e-8);
                var interpForSolvedStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble,
                   _interpolators.Select(x => x.Interpolate(solvedStrike)).ToArray(),
                   TimeInterpolatorType);
                vol = interpForSolvedStrike.Interpolate(maturity);
            }

            if (_allowCaching) _deltaVolCache[key] = vol;
            return vol;
        }

        private double GetAbsStrikeForDelta(double fwd, double deltaStrike, double maturity)
        {
            var cp = deltaStrike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (absK =>
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble, ExpiriesDouble.Select(e => GetVolForAbsoluteStrike(absK, e, fwd)).ToArray(), TimeInterpolatorType);
                var vol2 = interpForStrike.Interpolate(maturity);
                var deltaK = BlackFunctions.BlackDelta(fwd, absK, 0, maturity, vol2, cp);
                return deltaK - deltaStrike;
            });

            var solvedStrike = Math.Solvers.Brent.BrentsMethodSolve(testFunc, 0.000000001, 10 * fwd, 1e-8);

            return solvedStrike;
        }

        private double GetDeltaStrikeForAbs(double fwd, double strike, double maturity)
        {
            var cp = strike < 0 ? OptionType.Put : OptionType.Call;
            Func<double, double> testFunc = (deltaK =>
            {
                var interpForStrike = InterpolatorFactory.GetInterpolator(ExpiriesDouble, ExpiriesDouble.Select(e => GetVolForDeltaStrike(deltaK, e, fwd)).ToArray(), TimeInterpolatorType);
                var vol2 = interpForStrike.Interpolate(maturity);
                var absK = BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(fwd, deltaK, 0, maturity, vol2);
                return absK - strike;
            });

            var solvedStrike = -Math.Solvers.Brent.BrentsMethodSolve(testFunc, -0.999999999, -0.000000001, 1e-8);
            if (solvedStrike == 0.000000001 || solvedStrike == 0.999999999) //out of bounds
            {
                var upperK = testFunc(-0.000000001);
                var lowerK = testFunc(-0.999999999);
                if (System.Math.Abs(upperK - fwd) < System.Math.Abs(lowerK - fwd))
                    solvedStrike = 0.000000001;
                else
                    solvedStrike = 0.999999999;
            }

            return solvedStrike;
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

        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => GetForwardATMVol(TimeBasis.CalculateYearFraction(OriginDate, startDate), TimeBasis.CalculateYearFraction(OriginDate, endDate));

        public double GetForwardATMVol(double start, double end)
        {
            if (start > end)
                throw new Exception("Start must be strictly less than end");

            if (StrikeType==StrikeType.ForwardDelta)
            {
                if (start == end)
                    return GetVolForDeltaStrike(0.5,start,1.0);

                var vStart = GetVolForDeltaStrike(0.5, start, 1.0);
                vStart *= vStart * start;

                var vEnd = GetVolForDeltaStrike(0.5, end, 1.0);
                vEnd *= vEnd * end;

                var vDiff = vEnd - vStart;
                if (vDiff < 0)
                    throw new Exception("Negative forward variance detected");

                return System.Math.Sqrt(vDiff / (end - start));
            }

            throw new Exception("Only Forward-Delta type supported for fwd vol calcs");
        }

        public double Dvdk(double strike, DateTime expiry, double fwd)
        {
            if (StrikeType == StrikeType.ForwardDelta)
            {
                var t = TimeBasis.CalculateYearFraction(OriginDate, expiry);

                var pillarIx = Array.BinarySearch(Expiries, expiry);
                var interpForMaturity = pillarIx > 0 ?
                    _interpolators[pillarIx] :
                    InterpolatorFactory.GetInterpolator(Strikes, Strikes.Select(k => GetVolForDeltaStrike(k, expiry, fwd)).ToArray(), StrikeInterpolatorType);

                var deltaK = GetDeltaStrikeForAbs(fwd, strike, t);
                var vol = GetVolForAbsoluteStrike(strike, expiry, fwd);
                var gamma = BlackFunctions.BlackGamma(fwd, strike, 0.0, t, vol);
                return interpForMaturity.FirstDerivative(deltaK) * gamma;
            }
            else
            {
                var interpForMaturity = InterpolatorFactory.GetInterpolator(Strikes,
                    Strikes.Select(k => GetVolForAbsoluteStrike(k, expiry, fwd)).ToArray(),
                    StrikeInterpolatorType);
                return interpForMaturity.FirstDerivative(strike);
            }
        }

        public double Cdf(double strike, DateTime expiry, double fwd)
        {
            var t = TimeBasis.CalculateYearFraction(OriginDate, expiry);
            var vol = GetVolForAbsoluteStrike(strike, expiry, fwd);
            (var d1, var d2) = BlackFunctions.D1d2(fwd, strike, t, vol);
            var vega = BlackFunctions.BlackVega(fwd, strike, 0.0, t, vol);
            return NormSDist(-d2) + vega * Dvdk(strike, expiry, fwd);
        }

        public double InverseCDF(DateTime expiry, double fwd, double p)
        {
            var t = TimeBasis.CalculateYearFraction(OriginDate, expiry);
            var targetFunc = new Func<double, double>(k => p - Cdf(k, expiry, fwd));
            var minK = GetAbsStrikeForDelta(fwd, -1e-8, t);
            var maxK = GetAbsStrikeForDelta(fwd, -(1.0 - 1e-8), t);
            //var b = Math.Solvers.Newton1D.MethodSolve2(targetFunc, fwd, 1e-4, 1000, fwd * 0.0001);
            var b = Math.Solvers.Brent.BrentsMethodSolve(targetFunc, minK, maxK, 1e-8);
            return b;
        }
    }
}
