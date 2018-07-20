using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public class PriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = new DateTime[0];
        private readonly double[] _prices;
        private readonly PriceCurveType _curveType;
        private IInterpolator1D _interp;

        public DateTime BuildDate { get; private set; }

        public string Name { get; private set; }

        public int NumberOfPillars => _pillarDates.Length;

        public PriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, PriceCurveType curveType)
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
                case PriceCurveType.Linear:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.Linear);
                    break;
                case PriceCurveType.Next:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.NextValue);
                    break;
                case PriceCurveType.NextButOnExpiry:
                    pillarsAsDoubles.Select(x => x - 1).ToArray();
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.NextValue);
                    break;
                default:
                    throw new Exception($"Unkown price curve type {_curveType}");
            }
        }

        public double GetAveragePriceForDates(DateTime[] dates)
        {
            return _interp.Average(dates.Select(x=>x.ToOADate()));
        }

        public double GetPriceForDate(DateTime date)
        {
            return _interp.Interpolate(date.ToOADate());
        }


    }
}
