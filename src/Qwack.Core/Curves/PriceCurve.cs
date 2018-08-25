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

        private readonly string[] _pillarLabels;

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }

        public int NumberOfPillars => _pillarDates.Length;

        public PriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, PriceCurveType curveType, string[] pillarLabels=null)
        {
            BuildDate = buildDate;
            _pillarDates = PillarDates;
            _prices = Prices;
            _curveType = curveType;

            if (pillarLabels == null)
                _pillarLabels = _pillarDates.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                _pillarLabels = pillarLabels;

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

        public double GetAveragePriceForDates(DateTime[] dates) => _interp.Average(dates.Select(x => x.ToOADate()));

        public double GetPriceForDate(DateTime date) => _interp.Interpolate(date.ToOADate());

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize)
        {
            var o = new Dictionary<string, IPriceCurve>();
            for (var i = 0; i < _pillarDates.Length; i++)
            {
                var bumpedCurve = _prices.Select((x, ix) => ix == i ? x + bumpSize : x).ToArray();
                var c = new PriceCurve(BuildDate, _pillarDates, bumpedCurve, _curveType);
                var name = _pillarLabels[i];
                o.Add(name, c);
            }
            return o;
        }

        
    }
}
