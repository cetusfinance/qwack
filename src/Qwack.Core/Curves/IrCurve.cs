using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using static System.Math;

namespace Qwack.Core.Curves
{
    public class IrCurve : ICurve
    {
        private DateTime _buildDate;
        private readonly List<double> _pillarsD;
        private List<DateTime> _pillars;
        private List<double> _rates;
        private DayCountBasis _basis = DayCountBasis.Act_365F;
        private IInterpolator1D _interpolator;
        private Interpolator1DType _interpKind;

        public IrCurve(DateTime[] pillars, double[] rates, DateTime buildDate, string name, Interpolator1DType interpKind)
        {
            _interpKind = interpKind;
            _pillars = pillars.ToList();
            _rates = new List<double>();

            _pillarsD = new List<double>();
            _buildDate = buildDate;

            for (var i = 0; i < pillars.Count(); i++)
            {
                _pillarsD.Add(buildDate.CalculateYearFraction(pillars[i], _basis));
                _rates.Add(rates[i]);
            }

            _interpolator = InterpolatorFactory.GetInterpolator(_pillarsD.ToArray(), _rates.ToArray(), interpKind, isSorted: true, noCopy: true);
            Name = name;
        }


        public DateTime BuildDate => _buildDate;
        public string Name { get; internal set; }
        public int NumberOfPillars => _pillars.Count;
        public DateTime[] PillarDates => _pillars.ToArray();
        public Interpolator1DType InterpolatorType => _interpKind;
        public int SolveStage { get; set; }

        public double GetDf(DateTime startDate, DateTime endDate)
        {
            var ts = _buildDate.CalculateYearFraction(startDate, _basis);
            var te = _buildDate.CalculateYearFraction(endDate, _basis);
            var rateS = GetRate(ts);
            var rateE = GetRate(te);
            var dfS = Exp(-rateS * ts);
            var dfE = Exp(-rateE * te);
            return dfE / dfS;
        }
        public double GetRate(DateTime valueDate)
        {
            var T = _buildDate.CalculateYearFraction(valueDate, _basis);
            return GetRate(T);
        }

        public double GetRate(double T)
        {
            return _interpolator.Interpolate(T);
        }

        public double GetRate(int pillarIx)
        {
            return _rates[pillarIx];
        }
        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis)
        {
            var ccRate = GetForwardRate(startDate, endDate);
            var output = -1.0;

            var t365 = startDate.CalculateYearFraction(endDate, _basis);
            var cf = Exp(ccRate * t365);

            switch (rateType)
            {
                case RateType.Exponential:
                    output = Log(cf) / tbasis;
                    break;
                case RateType.Linear:
                    output = (cf - 1.0) / tbasis;
                    break;
                case RateType.DailyCompounded:
                case RateType.MonthlyCompounded:
                case RateType.YearlyCompounded:
                    throw new NotImplementedException();
            }
            return output;
        }
        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis)
        {
            var ccRate = GetForwardRate(startDate, endDate);
            var output = -1.0;

            var t365 = startDate.CalculateYearFraction(endDate, _basis);
            var tbas = startDate.CalculateYearFraction(endDate, basis);
            var cf = Exp(ccRate * t365);

            switch (rateType)
            {
                case RateType.Exponential:
                    output = Log(cf) / tbas;
                    break;
                case RateType.Linear:
                    output = (cf - 1.0) / tbas;
                    break;
                case RateType.DailyCompounded:
                case RateType.MonthlyCompounded:
                case RateType.YearlyCompounded:
                    throw new NotImplementedException();
            }
            return output;
        }

        public DateTime[] GetPillarDates()
        {
            return _pillars.ToArray();
        }

        public double[] GetRates()
        {
            return _rates.ToArray();
        }

        public double Pv(double fv, DateTime payDate)
        {
            var T = _buildDate.CalculateYearFraction(payDate, _basis);
            var rate = GetRate(T);
            var df = Exp(-rate * T);
            return fv * df;
        }

        public double GetForwardRate(DateTime startDate, DateTime endDate)
        {
            var te = _buildDate.CalculateYearFraction(endDate, _basis);
            var ts = _buildDate.CalculateYearFraction(startDate, _basis);
            var re = _interpolator.Interpolate(te);
            var rs = _interpolator.Interpolate(ts);
            var dFe = Exp(-te * re);
            var dFs = Exp(-ts * rs);

            var q = (1 / dFe) / (1 / dFs);

            return Log(q) / (te - ts);
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
                var returnCurve = new IrCurve(_pillars.ToArray(), _rates.Select((r, ix) => ix == pillarIx ? r + delta : r).ToArray(), _buildDate, Name, _interpKind);
                return returnCurve;
            }
        }

        public IrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            if (mutate)
            {
                _rates[pillarIx] = rate;
                _interpolator.UpdateY(pillarIx, rate, true);
                return this;
            }
            else
            {
                var returnCurve = new IrCurve(_pillars.ToArray(), _rates.Select((r, ix) => ix == pillarIx ? rate : r).ToArray(), _buildDate, Name, _interpKind);
                return returnCurve;
            }

        }
    }
}
