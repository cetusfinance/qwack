using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public class SparsePriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = new DateTime[0];
        private readonly double[] _prices;
        private readonly SparsePriceCurveType _curveType;
        private IInterpolator1D _interpA;
        private IInterpolator1D _interpB;


        public DateTime BuildDate { get; private set; }

        public string Name { get; private set; }

        public int NumberOfPillars => _pillarDates.Length;

        public SparsePriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, SparsePriceCurveType curveType)
        {
            BuildDate = buildDate;
            _pillarDates = PillarDates;
            _prices = Prices;
            _curveType = curveType;

            Initialize();
        }

        private void Initialize()
        {
            var pillarsAsDoubles = _pillarDates.Select(x => x.ToOADate()).ToArray();
            switch(_curveType)
            {
                case SparsePriceCurveType.Coal:
                    _interpA = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.Linear);
                    var allDates = _pillarDates.First().CalendarDaysInPeriod(_pillarDates.Last());
                    var monthlyDates = allDates.Select(x => x.NthLastSpecificWeekDay(DayOfWeek.Friday,1)).Distinct();
                    var monthlyPillars = monthlyDates.Select(x => x.ToOADate()).ToArray();
                    var monthlyPrices = _interpA.Many(monthlyPillars).ToArray();
                    _interpB = InterpolatorFactory.GetInterpolator(monthlyPillars, monthlyPrices, Interpolator1DType.NextValue);
                    break;
            }
        }

        public double GetAveragePriceForDates(DateTime[] dates)
        {
            return _interpB.Average(dates.Select(x=>x.ToOADate()));
        }

        public double GetPriceForDate(DateTime date)
        {
            return _interpB.Interpolate(date.ToOADate());
        }


    }
}
