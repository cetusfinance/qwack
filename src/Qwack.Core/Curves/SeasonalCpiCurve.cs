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
    public class SeasonalCpiCurve : CPICurve
    {
        public double[] SeasonalAdjustments { get; set; }
        
        public SeasonalCpiCurve() { }

        public SeasonalCpiCurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, InflationIndex inflationIndex, double[] seasonalAdjustments,
            CpiInterpolationType cpiInterpolationType = CpiInterpolationType.IndexLevel, DateTime? spotDate = null, double? spotFixing = null, Dictionary<DateTime, double> fixings = null ) 
        {
            BuildDate = buildDate;
            PillarDates = pillars;
            CpiRates = cpiRates;
            InflationIndex = inflationIndex;
            Basis = InflationIndex.DayCountBasis;
            CpiInterpolationType = cpiInterpolationType;
            SeasonalAdjustments = seasonalAdjustments;
            Fixings = fixings ?? new();
            if(spotDate.HasValue)
                SpotDate = spotDate.Value;
            if(spotFixing.HasValue)
                SpotFixing = spotFixing.Value;

            BuildInterpolator();
        }

        public SeasonalCpiCurve(DateTime buildDate, DateTime[] pillars, double[] cpiRates, DayCountBasis cpiBasis, Frequency cpiFixingLag, Calendar cpiCalendar,
            double[] seasonalAdjustments,CpiInterpolationType cpiInterpolationType = CpiInterpolationType.IndexLevel, DateTime? spotDate = null, double? spotFixing = null, Dictionary<DateTime, double> fixings = null)
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
            SeasonalAdjustments = seasonalAdjustments;
            Fixings = fixings ?? new();
            Basis = cpiBasis;
            CpiInterpolationType = cpiInterpolationType;
            if (spotDate.HasValue)
                SpotDate = spotDate.Value;
            if (spotFixing.HasValue)
                SpotFixing = spotFixing.Value;

            BuildInterpolator();
        }


        public SeasonalCpiCurve(TO_SeasonalCpiCurve transportObject, ICalendarProvider calendarProvider)
            : this(transportObject.BuildDate, transportObject.PillarDates, transportObject.CpiRates,  transportObject.Basis, 
                  new Frequency(transportObject.InflationIndex.FixingLag), calendarProvider.GetCalendarSafe(transportObject.InflationIndex.HolidayCalendars), 
                  transportObject.SeasonalAdjustments, transportObject.CpiInterpolationType, fixings: transportObject.Fixings)
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
                foreach (var kv in Fixings)
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
                    allCpiPoints[PillarDates[i]] = System.Math.Log(CpiRates[i] / SpotFixing) / t;
                }
                foreach (var kv in Fixings)
                {
                    var t = SpotDate.CalculateYearFraction(kv.Key, Basis);
                    allCpiPoints[kv.Key] = System.Math.Log(kv.Value / SpotFixing) / t;
                }
                var x = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
                var y = allCpiPoints.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

                _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
            }

            ApplySeasonality();
        }

        private void ApplySeasonality()
        {
            var lastFixingDate = Fixings.Keys.Max();
            var lastFixing = Fixings[lastFixingDate];
            var adjCurve = new Dictionary<DateTime, double>(Fixings);
            var lastPillar = PillarDates.Max();
            var d = lastFixingDate;
            var prevIndex = lastFixing;
            
            while (d < lastPillar)
            {
                d = d.AddMonths(1).FirstDayOfMonth();
                var fixing = GetForcastForLaggedDate(d);
                var ratio = System.Math.Log(fixing / prevIndex);
                var f = 12 * ratio;
                var s = SeasonalAdjustments[d.Month - 1] * 12;
                var adjForecast = prevIndex * System.Math.Exp((f + s) / 12);
                adjCurve[d] = adjForecast;
                prevIndex = adjForecast;
            }

            if (CpiInterpolationType == CpiInterpolationType.IndexLevel)
            {
             
                var x = adjCurve.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
                var y = adjCurve.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

                _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
            }
            else
            {
                var adjCurveRates = new Dictionary<DateTime, double>();
                foreach(var kv in adjCurve)
                {
                    var t = SpotDate.CalculateYearFraction(kv.Key, Basis);
                    adjCurveRates[kv.Key] = System.Math.Log(kv.Value / SpotFixing) / t;
                }
                
                var x = adjCurveRates.OrderBy(x => x.Key).Select(x => x.Key.ToOADate()).ToArray();
                var y = adjCurveRates.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

                _cpiInterp = InterpolatorFactory.GetInterpolator(x, y, InflationIndex.FixingInterpolation);
            }
        }

        public override IIrCurve BumpRate(int pillarIx, double delta, bool mutate)
        {
            if (mutate)
            {
                CpiRates[pillarIx] += delta;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new SeasonalCpiCurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? x + delta : x).ToArray(), InflationIndex, SeasonalAdjustments, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }
        public override IIrCurve BumpRateFlat(double delta, bool mutate)
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
                return new SeasonalCpiCurve(BuildDate, PillarDates, CpiRates.Select(x => x + delta).ToArray(), InflationIndex, SeasonalAdjustments, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }
     
        public override IIrCurve SetRate(int pillarIx, double rate, bool mutate)
        {
            if(mutate)
            {
                CpiRates[pillarIx] = rate;
                BuildInterpolator();
                return this;
            }
            else
            {
                return new SeasonalCpiCurve(BuildDate, PillarDates, CpiRates.Select((x, ix) => ix == pillarIx ? rate : x).ToArray(), InflationIndex, SeasonalAdjustments, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
                {
                    Name = Name,
                    CollateralSpec = CollateralSpec,
                    SolveStage = SolveStage,
                };
            }
        }

        public override IIrCurve RebaseDate(DateTime newAnchorDate)
        {
            var newCurve = new SeasonalCpiCurve(newAnchorDate, PillarDates, CpiRates, InflationIndex, SeasonalAdjustments, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
            {
                Name = Name,
                CollateralSpec = CollateralSpec,
                SolveStage = SolveStage,
            };

            return newCurve;
        }

        public new SeasonalCpiCurve Clone() => new(BuildDate, (DateTime[])PillarDates.Clone(), (double[])CpiRates.Clone(), InflationIndex, null, CpiInterpolationType, SpotDate, SpotFixing, Fixings)
        {
            Name = Name,
            SolveStage = SolveStage,
            CollateralSpec = CollateralSpec,
        };

        public new TO_SeasonalCpiCurve GetTransportObject() =>
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
               SeasonalAdjustments = SeasonalAdjustments,
               Fixings = Fixings,
           };
    }
}
