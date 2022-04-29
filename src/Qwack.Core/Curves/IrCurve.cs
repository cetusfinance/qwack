using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using static System.Math;

namespace Qwack.Core.Curves
{
    public class IrCurve : IIrCurve
    {
        private readonly DateTime _buildDate;
        private readonly DateTime[] _pillars;
        private readonly double[] _rates;
        private readonly DayCountBasis _basis = DayCountBasis.Act_365F;
        private readonly IInterpolator1D _interpolator;
        private readonly RateType _rateStorageType;
        internal readonly Interpolator1DType _interpKind;
        private readonly string _name;


        public IrCurve(DateTime[] pillars, double[] rates, DateTime buildDate, string name, Interpolator1DType interpKind, Currency ccy, string collateralSpec = null, RateType rateStorageType = RateType.Exponential)
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

        public IrCurve(TO_IrCurve transportObject, ICurrencyProvider currencyProvider)
            : this(transportObject.Pillars, transportObject.Rates, transportObject.BuildDate, transportObject.Name, transportObject.InterpKind,
                 currencyProvider.GetCurrency(transportObject.Ccy), transportObject.CollateralSpec, transportObject.RateStorageType) => _basis = transportObject.Basis;

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
            if (startDate == endDate)
                return 1.0;

            var ts = _buildDate.CalculateYearFraction(startDate, _basis);
            var te = _buildDate.CalculateYearFraction(endDate, _basis);
            return GetDf(ts, te);
        }

        public double GetDf(double tStart, double tEnd)
        {
            var rateS = GetRate(tStart);
            var rateE = GetRate(tEnd);
            var dfS = DFFromRate(tStart, rateS, RateStorageType);
            var dfE = DFFromRate(tEnd, rateE, RateStorageType);
            return dfE / dfS;
        }

        public double GetRate(DateTime valueDate)
        {
            var T = _buildDate.CalculateYearFraction(valueDate, _basis);
            return GetRate(T);
        }

        public static double DFFromRate(double t, double r, RateType rateType) => rateType switch
        {
            RateType.Exponential => Exp(-r * t),
            RateType.Linear => 1.0 / (1.0 + r * t),
            RateType.SemiAnnualCompounded => Pow(1.0 + r / 2.0, -2.0 * t),
            RateType.QuarterlyCompounded => Pow(1.0 + r / 4.0, -4.0 * t),
            RateType.MonthlyCompounded => Pow(1.0 + r / 12.0, -12.0 * t),
            RateType.YearlyCompounded => Pow(1.0 + r, t),
            RateType.DiscountFactor => r,
            _ => throw new NotImplementedException(),
        };

        public static double RateFromDF(double t, double df, RateType rateType) => rateType switch
        {
            RateType.Exponential => Log(df) / -t,
            RateType.Linear => (1.0 / df - 1.0) / t,
            RateType.SemiAnnualCompounded => (Pow(df, -1.0 / (2.0 * t)) - 1.0) * 2.0,
            RateType.QuarterlyCompounded => (Pow(df, -1.0 / (4.0 * t)) - 1.0) * 4.0,
            RateType.MonthlyCompounded => (Pow(df, -12.0 * t) - 1.0) * 12.0,
            RateType.YearlyCompounded => (Pow(df, -1.0 * t) - 1.0),
            RateType.DiscountFactor => df,
            _ => throw new NotImplementedException(),
        };

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
            var pillarsDropped = _pillars.Count(x => x <= newAnchorDate);
            var newLength = _pillars.Length - pillarsDropped;
            var newPillars = new DateTime[newLength];

            Array.Copy(_pillars, pillarsDropped, newPillars, 0, newLength);

            var dfAdjust = GetDf(BuildDate, newAnchorDate);
            var newDfs = newPillars.Select(x => GetDf(BuildDate, x) / dfAdjust).ToArray();
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

        public IrCurve Clone() => new((DateTime[])PillarDates.Clone(), (double[])_rates.Clone(), BuildDate, Name, _interpKind, Currency, CollateralSpec, RateStorageType)
        {
            SolveStage = SolveStage
        };

        public TO_IrCurve GetTransportObject() =>
            new()
            {
                Basis = Basis,
                BuildDate = BuildDate,
                Ccy = Currency.Ccy,
                CollateralSpec = CollateralSpec,
                InterpKind = InterpolatorType,
                Name = Name,
                Pillars = PillarDates,
                Rates = _rates,
                RateStorageType = RateStorageType
            };
    }
}
