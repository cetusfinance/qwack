using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class CPICurve : IIrCurve
    {
        public CPICurve() { }

        public CPICurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, InflationIndex inflationIndex) 
        {
            BuildDate = buildDate;
            PillarDates = pillars;
            CpiRates = cpiRates;
            InflationIndex = inflationIndex;
            Basis = InflationIndex.DayCountBasis;

            BuildInterpolator();
        }

        public CPICurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, DayCountBasis cpiBasis, Frequency cpiFixingLag, Calendar cpiCalendar)
        {
            BuildDate = buildDate;
            PillarDates = pillars;
            CpiRates = cpiRates;
            InflationIndex = new InflationIndex
            {
                DayCountBasis = cpiBasis,
                RollConvention = RollType.P,
                HolidayCalendars = cpiCalendar,
                FixingLag = cpiFixingLag,
                FixingInterpolation = Interpolator1DType.Linear
            };

            Basis = cpiBasis;

            BuildInterpolator();
        }

        private void BuildInterpolator()
        {
            var allCpiPoints = new Dictionary<DateTime, double>();

            for (var i = 0; i < PillarDates.Length; i++)
            {
                allCpiPoints[PillarDates[i]] = CpiRates[i];
            }
            var x = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
            var y = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

            _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
        }

        public DateTime BuildDate {get;set;}

        public DayCountBasis Basis { get; set;}

        public string Name { get; set; }

        public DateTime[] PillarDates { get; set; } = Array.Empty<DateTime>();
        public double[] CpiRates { get; set; } = Array.Empty<double>();

        public int NumberOfPillars => PillarDates.Length;

        public InflationIndex InflationIndex { get; set; }

        private IInterpolator1D _cpiInterp;

        public int SolveStage { get; set; }
        public string CollateralSpec { get; set; }

        public IIrCurve BumpRate(int pillarIx, double delta, bool mutate)
        {
            if (mutate)
            {
                CpiRates[pillarIx] += delta;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? x + delta : x).ToArray(), InflationIndex);
            }
        }
        public IIrCurve BumpRateFlat(double delta, bool mutate)
        {
            if (mutate)
            {
                for (var i = 0; i < PillarDates.Length; i++)
                {
                    CpiRates[i] += delta;
                }
                BuildInterpolator();
                return this;
            }
            else
            {
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select(x => x + delta).ToArray(), InflationIndex);
            }
        }
        public Dictionary<DateTime, IIrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate) => throw new NotImplementedException();


        public double GetForecast(DateTime fixingDate, int fixingLagInMonths) => _cpiInterp.Interpolate(fixingDate.AddMonths(fixingLagInMonths).ToOADate()); // InflationUtils.InterpFixing(fixingDate, _cpiInterp, fixingLagInMonths);
        public double GetForecastExact(DateTime fixingDate, int fixingLagInMonths) => InflationUtils.InterpFixing(fixingDate, _cpiInterp, fixingLagInMonths);

        public double GetDf(DateTime startDate, DateTime endDate)
        {
            var cpiStart = _cpiInterp.Interpolate(startDate.ToOADate());
            var cpiEnd = _cpiInterp.Interpolate(endDate.ToOADate());
            return cpiStart / cpiEnd;
        }

        public double GetDf(double tStart, double tEnd) => GetDf(BuildDate.AddYearFraction(tStart,DayCountBasis.Act_365F), BuildDate.AddYearFraction(tEnd,DayCountBasis.Act_365F)); 
        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis)
        {
            var tbas = startDate.CalculateYearFraction(endDate, basis);
            return GetForwardRate(startDate, endDate, rateType, tbas);
        }
        public double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis)
        {
            var df = GetDf(startDate, endDate);
            return IrCurve.RateFromDF(tbasis, df, rateType);
        }

        public double GetRate(int pillarIx) => CpiRates[pillarIx];
        public double GetRate(DateTime valueDate) => _cpiInterp.Interpolate(valueDate.ToOADate());
        public double[] GetSensitivity(DateTime valueDate) => throw new NotImplementedException();
        public IIrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            if(mutate)
            {
                CpiRates[pillarIx] = rate;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? rate : x).ToArray(), InflationIndex);
            }
        }

        public IIrCurve RebaseDate(DateTime newAnchorDate) => throw new NotImplementedException();

        public CPICurve Clone() => new(BuildDate, (DateTime[])PillarDates.Clone(), (double[])CpiRates.Clone(), InflationIndex)
        {
            Name = Name,
            SolveStage = SolveStage,
            CollateralSpec = CollateralSpec,
        };
    }
}
