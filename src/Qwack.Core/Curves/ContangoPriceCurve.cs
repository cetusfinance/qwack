using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Basic;

namespace Qwack.Core.Curves
{
    public class ContangoPriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = new DateTime[0];
        private readonly double[] _contangos;
        private readonly string[] _pillarLabels;
        private readonly double _spot;
        private readonly DateTime _spotDate;
        private readonly DayCountBasis _basis;
        private IInterpolator1D _interp;
        private ICurrencyProvider _currencyProvider;

        public PriceCurveType CurveType => PriceCurveType.Linear;

        public bool UnderlyingsAreForwards => false; //false as we only show spot delta

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }
        public string AssetId { get; set; }

        public Frequency SpotLag { get; set; } = new Frequency("2b");
        public Calendar SpotCalendar { get; set; }

        public DateTime[] PillarDates => _pillarDates;

        public int NumberOfPillars => _pillarDates.Length;

        public Currency Currency { get; set; }

        public ContangoPriceCurve(DateTime buildDate, double spot, DateTime spotDate, DateTime[] pillarDates, double[] contangos, ICurrencyProvider currencyProvider,
            DayCountBasis basis = DayCountBasis.ACT360, string[] pillarLabels = null)
        {
            _currencyProvider = currencyProvider;
            Currency = currencyProvider["USD"];
            BuildDate = buildDate;
            _pillarDates = pillarDates;
            _contangos = contangos;
            _basis = basis;
            _spot = spot;
            _spotDate = spotDate;

            if (pillarLabels == null)
                _pillarLabels = _pillarDates.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                _pillarLabels = pillarLabels;

            Initialize();
        }

        private void Initialize()
        {
            var pillarsAsDoubles = _pillarDates.Select(x => x.ToOADate()).ToArray();
            _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _contangos, Interpolator1DType.Linear);
        }

        public double GetAveragePriceForDates(DateTime[] dates) => dates.Average(d => GetFwd(d, _interp.Interpolate(d.ToOADate())));

        public double GetPriceForDate(DateTime date) => GetFwd(date, _interp.Interpolate(date.ToOADate()));
        public double GetPriceForFixingDate(DateTime date)
        {
            var prompt = date.AddPeriod(RollType.F, SpotCalendar, SpotLag);
            return GetFwd(prompt, _interp.Interpolate(prompt.ToOADate()));
        }

        private double GetFwd(DateTime fwdDate, double contango)
        {
            var t = _spotDate.CalculateYearFraction(fwdDate, _basis);
            return _spot * (1.0 + contango * t);
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();
            var cSpot = new ContangoPriceCurve(BuildDate, _spot + bumpSize, _spotDate, _pillarDates, _contangos, _currencyProvider, _basis)
            {
                SpotCalendar = SpotCalendar,
                SpotLag = SpotLag
            };
            o.Add("Spot", cSpot);
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
            var newSpot = GetPriceForDate(newSpotDate);
            var fwds = _pillarDates.Select(p => GetPriceForDate(p)).ToArray();
            var times = _pillarDates.Select(p => newSpotDate.CalculateYearFraction(p, _basis)).ToArray();
            var newCtgos = fwds.Select((f, ix) => (f / newSpot - 1.0) / times[ix]).ToArray();
            var o = new ContangoPriceCurve(newAnchorDate, newSpot, newSpotDate, _pillarDates, newCtgos, _currencyProvider, _basis, _pillarLabels)
            {
                AssetId = AssetId,
                Currency = Currency,
                Name = Name,
                SpotCalendar = SpotCalendar,
                SpotLag = SpotLag
            };
            return o;
        }

        public DateTime PillarDatesForLabel(string label)
        {
            if (label == "Spot")
                return _spotDate;
            var labelIx = Array.IndexOf(_pillarLabels, label);
            return _pillarDates[labelIx];
        }
    }
}
