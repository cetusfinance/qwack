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
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves
{
    public class CPICurve : IIrCurve
    {
        public CPICurve() { }

        public CPICurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, InflationIndex inflationIndex, 
            CpiInterpolationType cpiInterpolationType = CpiInterpolationType.IndexLevel, DateTime? spotDate = null, double? spotFixing = null, 
            Dictionary<DateTime, double> fixings = null) 
        {
            BuildDate = buildDate;
            PillarDates = pillars;
            CpiRates = cpiRates;
            InflationIndex = inflationIndex;
            Basis = InflationIndex.DayCountBasis;
            CpiInterpolationType = cpiInterpolationType;
            Fixings = fixings ?? new();
            if (spotDate.HasValue)
                SpotDate = spotDate.Value;
            if(spotFixing.HasValue)
                SpotFixing = spotFixing.Value;

            BuildInterpolator();
        }

        public CPICurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, DayCountBasis cpiBasis, Frequency cpiFixingLag, Calendar cpiCalendar, 
            CpiInterpolationType cpiInterpolationType = CpiInterpolationType.IndexLevel, DateTime? spotDate = null, double? spotFixing = null, Dictionary<DateTime, double> fixings = null)
        {
            BuildDate = buildDate;
            PillarDates = pillars;
            CpiRates = cpiRates;
            Fixings = fixings ?? new();
            InflationIndex = new InflationIndex
            {
                DayCountBasis = cpiBasis,
                RollConvention = RollType.P,
                HolidayCalendars = cpiCalendar,
                FixingLag = cpiFixingLag,
                FixingInterpolation = Interpolator1DType.Linear
            };

            Basis = cpiBasis;
            CpiInterpolationType = cpiInterpolationType;
            if (spotDate.HasValue)
                SpotDate = spotDate.Value;
            if (spotFixing.HasValue)
                SpotFixing = spotFixing.Value;

            BuildInterpolator();
        }


        public CPICurve(TO_CPICurve transportObject, ICalendarProvider calendarProvider)
            : this(transportObject.BuildDate, transportObject.PillarDates, transportObject.CpiRates, transportObject.Basis, new Frequency(transportObject.InflationIndex.FixingLag), calendarProvider.GetCalendarSafe(transportObject.InflationIndex.HolidayCalendars), transportObject.CpiInterpolationType, fixings: transportObject.Fixings)
        {
            Name = transportObject.Name;
            if (transportObject.SpotDate != default)
                SpotDate = transportObject.SpotDate;

            SolveStage = transportObject.SolveStage;

            if (transportObject.SpotFixing != default)
                SpotFixing = transportObject.SpotFixing;
        }



        private void BuildInterpolator()
        {
            if (CpiInterpolationType == CpiInterpolationType.IndexLevel)
            {
                var allCpiPoints = new Dictionary<DateTime, double>();

                for (var i = 0; i < PillarDates.Length; i++)
                {
                    allCpiPoints[PillarDates[i]] = CpiRates[i];
                }
                foreach(var kv in Fixings)
                {
                    allCpiPoints[kv.Key] = kv.Value;
                }
                var x = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
                var y = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

                _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
            }
            else
            {
                var allCpiPoints = new Dictionary<DateTime, double>();
                if (SpotDate == default)
                {
                    var daysInMonth = BuildDate.DaysInMonth();
                    var monthFrac = ((double)BuildDate.Day) / daysInMonth;
                    var spotMonth = BuildDate.AddMonths(-System.Math.Abs(InflationIndex.FixingLag.PeriodCount));
                    var daysInSpotMonth = (double)spotMonth.DaysInMonth();
                    SpotDate = spotMonth.AddDays(daysInSpotMonth * monthFrac);
                }
                for (var i = 0; i < PillarDates.Length; i++)
                {
                    var t = SpotDate.CalculateYearFraction(PillarDates[i], Basis);
                    allCpiPoints[PillarDates[i]] = System.Math.Log(CpiRates[i]/SpotFixing) / t;
                }
                foreach (var kv in Fixings)
                {
                    var t = SpotDate.CalculateYearFraction(kv.Key, Basis);
                    allCpiPoints[kv.Key] = t == 0 ? 0 : System.Math.Log(kv.Value / SpotFixing) / t;
                }
                var x = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
                var y = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

                _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
            }
        }

        public DateTime BuildDate {get;set;}
        public Dictionary<DateTime, double> Fixings { get; set; }

        public DayCountBasis Basis { get; set; } = DayCountBasis.Act_365F;
        public CpiInterpolationType CpiInterpolationType { get; set; } = CpiInterpolationType.IndexLevel;
        public double SpotFixing { get; set; }
        public DateTime SpotDate { get; set; }
        public string Name { get; set; }

        public DateTime[] PillarDates { get; set; } = Array.Empty<DateTime>();
        public double[] CpiRates { get; set; } = Array.Empty<double>();

        public int NumberOfPillars => PillarDates.Length;

        public InflationIndex InflationIndex { get; set; }

        protected IInterpolator1D _cpiInterp;

        public int SolveStage { get; set; }
        public string CollateralSpec { get; set; }

        public virtual IIrCurve BumpRate(int pillarIx, double delta, bool mutate)
        {
            if (mutate)
            {
                CpiRates[pillarIx] += delta;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? x + delta : x).ToArray(), InflationIndex, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }
        public virtual IIrCurve BumpRateFlat(double delta, bool mutate)
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
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select(x => x + delta).ToArray(), InflationIndex, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }
        public virtual Dictionary<DateTime, IIrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate) => throw new NotImplementedException();


        public virtual double GetForecast(DateTime fixingDate, int fixingLagInMonths)
        {
            var efDate = fixingDate.AddMonths(-System.Math.Abs(fixingLagInMonths));
            return GetForcastForLaggedDate(efDate);
        }

        public virtual double GetForcastForLaggedDate(DateTime laggedDate)
        {
            if (CpiInterpolationType == CpiInterpolationType.IndexLevel)
                return _cpiInterp.Interpolate(laggedDate.ToOADate()); 
            else
            {
                var t = SpotDate.CalculateYearFraction(laggedDate, Basis);
                var r = _cpiInterp.Interpolate(laggedDate.ToOADate());
                return SpotFixing * System.Math.Exp(r * t);
            }
        }

        public virtual double GetForecastExact(DateTime fixingDate, int fixingLagInMonths)
        {
            if (CpiInterpolationType == CpiInterpolationType.IndexLevel)
                return InflationUtils.InterpFixing(fixingDate, _cpiInterp, fixingLagInMonths);
            else
            {
                return InflationUtils.InterpFixing(fixingDate, GetForcastForLaggedDate, fixingLagInMonths);
            }
        }

        public virtual double GetDf(DateTime startDate, DateTime endDate)
        {

            var cpiStart = GetForecast(startDate, InflationIndex.FixingLag.PeriodCount);
            var cpiEnd = GetForecast(endDate, InflationIndex.FixingLag.PeriodCount);
            return cpiStart / cpiEnd;
        }

        public virtual double GetDf(double tStart, double tEnd) => GetDf(BuildDate.AddYearFraction(tStart,DayCountBasis.Act_365F), BuildDate.AddYearFraction(tEnd,DayCountBasis.Act_365F)); 
        public virtual double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis)
        {
            var tbas = startDate.CalculateYearFraction(endDate, basis);
            return GetForwardRate(startDate, endDate, rateType, tbas);
        }
        public  virtual double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis)
        {
            var df = GetDf(startDate, endDate);
            return IrCurve.RateFromDF(tbasis, df, rateType);
        }

        public virtual double GetRate(int pillarIx) => CpiRates[pillarIx];
        public virtual double GetRate(DateTime valueDate) => _cpiInterp.Interpolate(valueDate.ToOADate());
        public virtual double[] GetSensitivity(DateTime valueDate) => throw new NotImplementedException();
        public virtual IIrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            if(mutate)
            {
                CpiRates[pillarIx] = rate;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new CPICurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? rate : x).ToArray(), InflationIndex, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }

        public virtual IIrCurve RebaseDate(DateTime newAnchorDate)
        {
            var newCurve = new CPICurve(newAnchorDate, PillarDates, CpiRates, InflationIndex, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
            {
                Name = Name,
                CollateralSpec = CollateralSpec,
                SolveStage = SolveStage,
            };

            return newCurve;
        }

        public virtual CPICurve Clone() => new(BuildDate, (DateTime[])PillarDates.Clone(), (double[])CpiRates.Clone(), InflationIndex, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
        {
            Name = Name,
            SolveStage = SolveStage,
            CollateralSpec = CollateralSpec,
        };

        public virtual TO_CPICurve GetTransportObject() =>
           new()
           {
               Basis = Basis,
               BuildDate = BuildDate,
               CollateralSpec = CollateralSpec,
               Name = Name,
               PillarDates = (DateTime[])PillarDates.Clone(),
               CpiRates = (double[])CpiRates.Clone(),
               SolveStage=SolveStage,
               InflationIndex = InflationIndex.GetTransportObject(),
               CpiInterpolationType = CpiInterpolationType,
               SpotFixing = SpotFixing,
               SpotDate = SpotDate,
               Fixings = Fixings
           };
    }
}
