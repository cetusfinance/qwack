using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using static System.Math;

namespace Qwack.Core.Curves
{
    public class IrCurve : IIrCurve
    {
        private DateTime _buildDate;
        private readonly DateTime[] _pillars;
        private readonly double[] _rates;
        private readonly DayCountBasis _basis = DayCountBasis.Act_365F;
        private readonly IInterpolator1D _interpolator;
        private readonly RateType _rateStorageType;
        internal readonly Interpolator1DType _interpKind;
        private readonly string _name;

        public IrCurve(DateTime[] pillars, double[] rates, DateTime buildDate, string name, Interpolator1DType interpKind, Currency ccy, string collateralSpec=null, RateType rateStorageType = RateType.Exponential)
        {
            _interpKind = interpKind;
            _rateStorageType = rateStorageType;
            _pillars = new DateTime[pillars.Length];
            pillars.CopyTo(_pillars, 0);
            _rates = new double[_pillars.Length];

            var pillarsD = new double[_pillars.Length];
            _buildDate = buildDate;

            for (var i = 0; i < pillars.Length; i++)
            {
                pillarsD[i] = buildDate.CalculateYearFraction(pillars[i], _basis);
                _rates[i] = rates[i];
            }

            _interpolator = InterpolatorFactory.GetInterpolator(pillarsD.ToArray(), _rates.ToArray(), interpKind, isSorted: true, noCopy: true);
            _name = name;
            Currency = ccy;
            CollateralSpec = collateralSpec ?? (string.IsNullOrWhiteSpace(_name) ? null :
                (_name.Contains("[")) ? _name.Split('[').Last().Trim("[]".ToCharArray()) : _name.Split('.').Last());
        }

        public DateTime BuildDate => _buildDate;
        public string Name => _name;
        public int NumberOfPillars => _pillars.Length;
        public DateTime[] PillarDates => _pillars;
        public Interpolator1DType InterpolatorType => _interpKind;

        public RateType RateStorageType => _rateStorageType;

        public int SolveStage { get; set; }
        public DayCountBasis Basis => _basis;
        public Currency Currency;

        public string CollateralSpec { get; private set; }
        public FloatRateIndex RateIndex { get; private set; }

        public void SetCollateralSpec(string collateralSpec) => CollateralSpec = collateralSpec;

        public void SetRateIndex(FloatRateIndex rateIndex) => RateIndex = rateIndex;

    
        public double GetDf(DateTime startDate, DateTime endDate)
        {
            var ts = _buildDate.CalculateYearFraction(startDate, _basis);
            var te = _buildDate.CalculateYearFraction(endDate, _basis);
            var rateS = GetRate(ts);
            var rateE = GetRate(te);
            var dfS = DFFromRate(ts, rateS, RateStorageType);
            var dfE = DFFromRate(te, rateE, RateStorageType);
            return dfE / dfS;
        }

        public double GetRate(DateTime valueDate)
        {
            var T = _buildDate.CalculateYearFraction(valueDate, _basis);
            return GetRate(T);
        }

        public static double DFFromRate(double t, double r, RateType rateType)
        {
            switch (rateType)
            {
                case RateType.Exponential:
                    return Exp(-r * t);
                case RateType.Linear:
                    return 1.0 / (1.0 + r * t);
                case RateType.SemiAnnualCompounded:
                    return Pow(1.0 + r / 2.0, -2.0 * t);
                case RateType.QuarterlyCompounded:
                    return Pow(1.0 + r / 4.0, -4.0 * t);
                case RateType.MonthlyCompounded:
                    return Pow(1.0 + r / 12.0, -12.0 * t);
                case RateType.YearlyCompounded:
                    return Pow(1.0 + r, t);
                case RateType.DiscountFactor:
                    return r;
                default:
                    throw new NotImplementedException();
            }
        }

        public static double RateFromDF(double t, double df, RateType rateType)
        {
            switch (rateType)
            {
                case RateType.Exponential:
                    return Log(df)/-t;
                case RateType.Linear:
                    return (1.0 / df - 1.0) / t;
                case RateType.SemiAnnualCompounded:
                    return (Pow(df, -1.0 / (2.0 * t)) - 1.0) * 2.0;
                case RateType.QuarterlyCompounded:
                    return (Pow(df, -1.0 / (4.0 * t)) - 1.0) * 4.0;
                case RateType.MonthlyCompounded:
                    return (Pow(df, -12.0 * t) - 1.0) * 12.0;
                case RateType.YearlyCompounded:
                    return (Pow(df, - 1.0 * t) - 1.0);
                case RateType.DiscountFactor:
                    return df;
                default:
                    throw new NotImplementedException();
            }
        }

        public double GetRate(double T) => _interpolator.Interpolate(T);

        public double GetRate(int pillarIx) => _rates[pillarIx];

        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis)
        {
            var df = GetDf(startDate, endDate);
            return RateFromDF(tbasis, df, rateType);
        }
        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis)
        {
            var tbas = startDate.CalculateYearFraction(endDate, basis);
            return GetForwardRate(startDate, endDate, rateType, tbas);
        }

        public double GetForwardCCRate(DateTime startDate, DateTime endDate)
        {
            var te = _buildDate.CalculateYearFraction(endDate, _basis);
            var ts = _buildDate.CalculateYearFraction(startDate, _basis);
            var q = GetDf(startDate, endDate);
            return -Log(q) / (te - ts);
        }

        public double[] GetRates() => _rates.ToArray();

        public double Pv(double fv, DateTime payDate)
        {
            var T = _buildDate.CalculateYearFraction(payDate, _basis);
            var rate = GetRate(T);
            var df = Exp(-rate * T);
            return fv * df;
        }

        public IrCurve BumpRate(int pillarIx, double delta, bool mutate)
        {
            if (mutate)
            {
                _rates[pillarIx] += delta;
                _interpolator.UpdateY(pillarIx, _rates[pillarIx], true);
                return this;
            }
            else
            {
                var returnCurve = new IrCurve(_pillars.ToArray(), _rates.Select((r, ix) => ix == pillarIx ? r + delta : r).ToArray(), _buildDate, _name, _interpKind, Currency, CollateralSpec, RateStorageType)
                {
                    SolveStage = SolveStage,
                    RateIndex = RateIndex,
                };
                return returnCurve;
            }
        }

        public IIrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            if (mutate)
            {
                _rates[pillarIx] = rate;
                _interpolator.UpdateY(pillarIx, rate, true);
                return this;
            }
            else
            {
                var returnCurve = new IrCurve(_pillars.ToArray(), _rates.Select((r, ix) => ix == pillarIx ? rate : r).ToArray(), _buildDate, _name, _interpKind, Currency, CollateralSpec, RateStorageType);
                return returnCurve;
            }

        }

        public double[] GetSensitivity(DateTime valueDate)
        {
            var T = _buildDate.CalculateYearFraction(valueDate, _basis);
            return _interpolator.Sensitivity(T);
        }

        public IrCurve RebaseDate(DateTime newAnchorDate)
        {
            var pillarsDropped = _pillars.Count(x => x < newAnchorDate);
            var newLength =  _pillars.Length - pillarsDropped;
            var newPillars = new DateTime[newLength];

            Array.Copy(_pillars, pillarsDropped, newPillars, 0, newLength);

            var dfAdjust = GetDf(BuildDate, newAnchorDate);
            var newDfs = newPillars.Select(x => GetDf(BuildDate, x)/ dfAdjust).ToArray();
            var newRates = newDfs.Select((x, ix) => RateFromDF(newAnchorDate.CalculateYearFraction(newPillars[ix], _basis), x, RateStorageType)).ToArray();
            if (newPillars.First() == newAnchorDate && newRates.Length > 1)
                newRates[0] = newRates[1];

            var newCurve = new IrCurve(newPillars, newRates, newAnchorDate, Name, _interpKind, Currency, CollateralSpec, RateStorageType);

            return newCurve;
        }

        public IrCurve BumpRateFlat(double delta, bool mutate)
        {
            if (mutate)
            {
                for (var i = 0; i < _rates.Length; i++)
                {
                    _rates[i] += delta;
                    _interpolator.UpdateY(i, _rates[i], true);
                }
                return this;

            }
            else
            {
                var returnCurve = new IrCurve(_pillars.ToArray(), _rates.Select(r => r + delta).ToArray(), _buildDate, _name, _interpKind, Currency, CollateralSpec, RateStorageType);
                return returnCurve;
            }
        }

        public Dictionary<DateTime, IrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate)
        {
            var o = new Dictionary<DateTime, IrCurve>();

            var lastBumpIx = _pillars.Length;

            var ix = Array.BinarySearch(_pillars, lastSensitivityDate);
            ix = (ix < 0) ? ~ix : ix;
            ix += 3;
            lastBumpIx = Min(ix, lastBumpIx); //cap at last pillar

            for (var i = 0; i < lastBumpIx; i++)
            {
                o.Add(PillarDates[i], BumpRate(i, delta, false));
            }
            return o;
        }

        public IrCurve Clone() => new IrCurve((DateTime[])PillarDates.Clone(), (double[])_rates.Clone(), BuildDate, Name, _interpKind, Currency, CollateralSpec, RateStorageType)
        {
            SolveStage = SolveStage
        };
    }
}
