using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Core.Descriptors;

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

        public bool UnderlyingsAreForwards => true;

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }
        public string AssetId { get; set; }

        public Frequency SpotLag { get; set; } = new Frequency("0b");
        public Calendar SpotCalendar { get; set; }

        public DateTime[] PillarDates => _pillarDates;

        public int NumberOfPillars => _pillarDates.Length;

        public Currency Currency { get; set; } = new Currency("USD", DayCountBasis.ACT360, null);

        public List<MarketDataDescriptor> Descriptors => new List<MarketDataDescriptor>()
            {
                    new AssetCurveDescriptor {
                        AssetId =AssetId,
                        Currency =Currency,
                        Name =Name,
                        ValDate =BuildDate}
            };
        public List<MarketDataDescriptor> Dependencies => new List<MarketDataDescriptor>();


        public ContangoPriceCurve(DateTime buildDate, double spot, DateTime spotDate, DateTime[] pillarDates, double[] contangos, DayCountBasis basis = DayCountBasis.ACT360, string[] pillarLabels = null)
        {
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

        private double GetFwd(DateTime fwdDate, double contango)
        {
            var t = _spotDate.CalculateYearFraction(fwdDate, _basis);
            return _spot * (1.0 + contango * t);
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();
            var cSpot = new ContangoPriceCurve(BuildDate, _spot+bumpSize, _spotDate, _pillarDates, _contangos, _basis);
            o.Add("Spot", cSpot);
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
            var newSpot = GetPriceForDate(newSpotDate);
            var o = new ContangoPriceCurve(newAnchorDate, newSpot, newSpotDate, _pillarDates, _contangos, _basis, _pillarLabels);
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
