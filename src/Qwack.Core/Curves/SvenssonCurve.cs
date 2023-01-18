using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using static System.Math;

namespace Qwack.Core.Curves
{
    public class SvenssonCurve : IIrCurve
    {
        private readonly DateTime _buildDate;
        private readonly DayCountBasis _basis = DayCountBasis.Act_365F;
        private readonly string _name;


        public SvenssonCurve(double beta0, double beta1, double beta2, double beta3, double tau1, double tau2, DateTime buildDate, string name, Currency ccy, string collateralSpec = null)
        {
            Beta0 = beta0;
            Beta1 = beta1;
            Beta2 = beta2;
            Beta3 = beta3;
            Tau1 = tau1;
            Tau2 = tau2;
            _buildDate = buildDate;

            _name = name;
            Currency = ccy;
            CollateralSpec = collateralSpec ?? (string.IsNullOrWhiteSpace(_name) ? null :
                (_name.Contains("[")) ? _name.Split('[').Last().Trim("[]".ToCharArray()) : _name.Split('.').Last());
        }

        public DateTime BuildDate => _buildDate;
        public string Name => _name;
        public int NumberOfPillars => 0;
        public DateTime[] PillarDates => new DateTime[0];

        public DayCountBasis Basis => _basis;

        public RateType RateStorageType => RateType.Exponential;

        public Currency Currency;

        public double Beta0 { get; }
        public double Beta1 { get; }
        public double Beta2 { get; }
        public double Beta3 { get; }
        public double Tau1 { get; }
        public double Tau2 { get; }
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

        public double GetRate(double T) => Beta0 + Beta1 * (1 - Exp(-T / Tau1)) / (T / Tau1) + Beta2 * ((1 - Exp(-T / Tau1)) / (T / Tau1) - Exp(-T / Tau1)) + Beta3 * ((1 - Exp(-T / Tau2)) / (T / Tau2) - Exp(-T / Tau2));

        public double GetRate(int pillarIx) => GetRate(0);

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

        public double[] GetRates() => new double[0];

        public double Pv(double fv, DateTime payDate)
        {
            var T = _buildDate.CalculateYearFraction(payDate, _basis);
            var rate = GetRate(T);
            var df = Exp(-rate * T);
            return fv * df;
        }

        public IrCurve BumpRate(int pillarIx, double delta, bool mutate)
        {
            throw new NotImplementedException();
        }

        public IIrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            throw new NotImplementedException();
        }

        public double[] GetSensitivity(DateTime valueDate)
        {
            throw new NotImplementedException();
        }

        public IrCurve RebaseDate(DateTime newAnchorDate)
        {
            throw new NotImplementedException();
        }

        public IrCurve BumpRateFlat(double delta, bool mutate)
        {
            throw new NotImplementedException();
        }

        public Dictionary<DateTime, IrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate)
        {
            throw new NotImplementedException();
        }

        public SvenssonCurve Clone() => new SvenssonCurve(Beta0, Beta1, Beta2, Beta3, Tau1, Tau2, BuildDate, Name, Currency, CollateralSpec);
        IIrCurve IIrCurve.BumpRate(int pillarIx, double delta, bool mutate) => throw new NotImplementedException();
        IIrCurve IIrCurve.BumpRateFlat(double delta, bool mutate) => throw new NotImplementedException();
        Dictionary<DateTime, IIrCurve> IIrCurve.BumpScenarios(double delta, DateTime lastSensitivityDate) => throw new NotImplementedException();
        IIrCurve IIrCurve.RebaseDate(DateTime newAnchorDate) => throw new NotImplementedException();
    }
}
