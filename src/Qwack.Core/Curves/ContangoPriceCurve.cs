using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves
{
    public class ContangoPriceCurve : IPriceCurve
    {
        private IInterpolator1D _interp;
        private readonly ICurrencyProvider _currencyProvider;

        public PriceCurveType CurveType => PriceCurveType.Linear;
        public CommodityUnits Units { get; set; } = CommodityUnits.troyOunce;

        public bool UnderlyingsAreForwards => false; //false as we only show spot delta

        public DateTime BuildDate { get; private set; }
        public DateTime RefDate => SpotDate;

        public string Name { get; set; }
        public string AssetId { get; set; }

        public Frequency SpotLag { get; set; } = new Frequency("2b");
        public Calendar SpotCalendar { get; set; }

        public double Spot { get; }
        public DateTime SpotDate { get; }
        public double[] Contangos { get; }
        public DayCountBasis Basis { get; }
        public string[] PillarLabels { get; }
        public DateTime[] PillarDates { get; }

        public int NumberOfPillars => PillarDates.Length;

        public Currency Currency { get; set; }

        public ICurrencyProvider CurrencyProvider => _currencyProvider;

        public ContangoPriceCurve(DateTime buildDate, double spot, DateTime spotDate, DateTime[] pillarDates, double[] contangos, ICurrencyProvider currencyProvider,
            DayCountBasis basis = DayCountBasis.ACT360, string[] pillarLabels = null)
        {
            _currencyProvider = currencyProvider;
            Currency = currencyProvider["USD"];
            BuildDate = buildDate;
            PillarDates = pillarDates;
            Contangos = contangos;
            Basis = basis;
            Spot = spot;
            SpotDate = spotDate;

            PillarLabels = pillarLabels ?? PillarDates.Select(x => x.ToString("yyyy-MM-dd")).ToArray();

            Initialize();
        }

        public ContangoPriceCurve(TO_ContangoPriceCurve transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
            : this(transportObject.BuildDate, transportObject.Spot, transportObject.SpotDate, transportObject.PillarDates, transportObject.Contangos,
                 currencyProvider, transportObject.Basis, transportObject.PillarLabels)
        {
            AssetId = transportObject.AssetId;
            Name = transportObject.Name;
            Currency = currencyProvider.GetCurrencySafe(transportObject.Currency);
            SpotCalendar = calendarProvider.GetCalendarSafe(transportObject.SpotCalendar);
        }

        private void Initialize()
        {
            var pillarsAsDoubles = PillarDates.Select(x => x.ToOADate()).ToArray();
            _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, Contangos, Interpolator1DType.Linear);
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
            var t = SpotDate.CalculateYearFraction(fwdDate, Basis);
            return Spot * (1.0 + contango * t);
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump, DateTime[] sparsePointsToBump = null)
        {
            var o = new Dictionary<string, IPriceCurve>();
            var cSpot = new ContangoPriceCurve(BuildDate, Spot + bumpSize, SpotDate, PillarDates, Contangos, _currencyProvider, Basis)
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
            var fwds = PillarDates.Select(p => GetPriceForDate(p)).ToArray();
            var times = PillarDates.Select(p => newSpotDate.CalculateYearFraction(p, Basis)).ToArray();
            var newCtgos = fwds.Select((f, ix) => times[ix] == 0 ? 0.0 : (f / newSpot - 1.0) / times[ix]).ToArray();

            var o = new ContangoPriceCurve(newAnchorDate, newSpot, newSpotDate, PillarDates, newCtgos, _currencyProvider, Basis, PillarLabels)
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
                return SpotDate;
            var labelIx = Array.IndexOf(PillarLabels, label);
            return PillarDates[labelIx];
        }
    }
}
