using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;

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

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }

        public int NumberOfPillars => _pillarDates.Length;

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

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize)
        {
            var o = new Dictionary<string, IPriceCurve>();
            
            //spot scenario first
            var cSpot = new ContangoPriceCurve(BuildDate, _spot+bumpSize, _spotDate, _pillarDates, _contangos, _basis);
            o.Add("Spot", cSpot);

            for (var i = 0; i < _pillarDates.Length; i++)
            {
                var t = _spotDate.CalculateYearFraction(_pillarDates[i], _basis);
                var deltaContango = bumpSize / _spot / t;  
                var bumpedCurve = _contangos.Select((x, ix) => ix == i ? x + deltaContango : x).ToArray();
                var c = new ContangoPriceCurve(BuildDate, _spot, _spotDate, _pillarDates, bumpedCurve, _basis); 
                var name = _pillarLabels[i];
                o.Add(name, c);
            }
            return o;
        }
    }
}
