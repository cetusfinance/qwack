using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class SparsePriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = Array.Empty<DateTime>();
        private readonly double[] _prices;
        private readonly string[] _pillarLabels;

        private readonly SparsePriceCurveType _curveType;
        private IInterpolator1D _interpA;
        private IInterpolator1D _interpB;
        private ICurrencyProvider _currencyProvider;

        public bool UnderlyingsAreForwards => _curveType == SparsePriceCurveType.Coal;

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }
        public string AssetId { get; set; }

        public int NumberOfPillars => _pillarDates.Length;

        public Currency Currency { get; set; } 

        public Calendar SpotCalendar { get; set; }
        public Frequency SpotLag { get; set; }

        public DateTime[] PillarDates => _pillarDates;

        public PriceCurveType CurveType => throw new NotImplementedException();

        public SparsePriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, SparsePriceCurveType curveType, ICurrencyProvider currencyProvider, string[] pillarLabels=null)
        {
            _currencyProvider = currencyProvider;
            Currency = currencyProvider["USD"];
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
                case SparsePriceCurveType.Coal:
                    _interpA = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.LinearFlatExtrap);
                    var allDates = _pillarDates.First().CalendarDaysInPeriod(_pillarDates.Last());
                    var monthlyDates = allDates.Select(x => x.NthLastSpecificWeekDay(DayOfWeek.Friday,1)).Distinct();
                    var monthlyPillars = monthlyDates.Select(x => x.ToOADate()).ToArray();
                    var monthlyPrices = _interpA.Many(monthlyPillars).ToArray();
                    _interpB = InterpolatorFactory.GetInterpolator(monthlyPillars, monthlyPrices, Interpolator1DType.NextValue);
                    break;
            }
        }

        public double GetAveragePriceForDates(DateTime[] dates) => _interpB.Average(dates.Select(x => x.ToOADate()));

        public double GetPriceForDate(DateTime date) => _interpB.Interpolate(date.ToOADate());
        public double GetPriceForFixingDate(DateTime date) => _interpB.Interpolate(date.AddPeriod(RollType.F, SpotCalendar, SpotLag).ToOADate());

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();
            for (var i = 0; i < _pillarDates.Length; i++)
            {
                var bumpedCurve = _prices.Select((x, ix) => ix == i ? x + bumpSize : x).ToArray();
                var c = new SparsePriceCurve(BuildDate, _pillarDates, bumpedCurve, _curveType, _currencyProvider, null);
                var name = _pillarLabels[i];
                o.Add(name, c);
            }
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            if (_pillarDates.First() < newAnchorDate) //remove first point as it has expired tomorrow
            {
                var newPillars = ((DateTime[])_pillarDates.Clone()).ToList();
                newPillars.RemoveAt(0);
                var newPrices = ((double[])_prices.Clone()).ToList();
                newPrices.RemoveAt(0);
                return new SparsePriceCurve(newAnchorDate, newPillars.ToArray(), newPrices.ToArray(), _curveType, _currencyProvider, _pillarLabels);
            }
            else
            {
                return new SparsePriceCurve(newAnchorDate, _pillarDates, _prices, _curveType, _currencyProvider, _pillarLabels);
            }
        }

        public DateTime PillarDatesForLabel(string label)
        {
            var labelIx = Array.IndexOf(_pillarLabels, label);
            return _pillarDates[labelIx];
        }
    }
}
