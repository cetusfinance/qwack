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
    public class PriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = new DateTime[0];
        private readonly double[] _prices;
        private readonly PriceCurveType _curveType;
        private IInterpolator1D _interp;
        private ICurrencyProvider _currencyProvider;

        public bool UnderlyingsAreForwards => _curveType == PriceCurveType.LME;

        public PriceCurveType CurveType => _curveType;

        private readonly string[] _pillarLabels;

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }
        public string AssetId { get; set; }

        public Frequency SpotLag { get; set; } = new Frequency("0b");
        public Calendar SpotCalendar { get; set; }

        public int NumberOfPillars => _pillarDates.Length;
        public DateTime[] PillarDates => _pillarDates;
        public double[] Prices => _prices;
        public ICurrencyProvider CurrencyProvider => _currencyProvider;
        public string[] PillarLabels => _pillarLabels;

        public Currency Currency { get; set; }
        public string CollateralSpec { get; set; }

        public PriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, PriceCurveType curveType, ICurrencyProvider currencyProvider, string[] pillarLabels = null)
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
            switch (_curveType)
            {
                case PriceCurveType.Linear:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.Linear);
                    break;
                case PriceCurveType.Next:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.NextValue);
                    break;
                case PriceCurveType.NextButOnExpiry:
                    pillarsAsDoubles = pillarsAsDoubles.Select(x => x - 1).ToArray();
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.NextValue);
                    break;
                case PriceCurveType.Constant:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.DummyPoint);
                    break;
                default:
                    throw new Exception($"Unkown price curve type {_curveType}");
            }
        }

        public double GetAveragePriceForDates(DateTime[] dates) => _interp.Average(dates.Select(x => x.ToOADate()));

        public double GetPriceForDate(DateTime date) => _interp.Interpolate(date.ToOADate());
        public double GetPriceForFixingDate(DateTime date) => _interp.Interpolate(date.AddPeriod(RollType.F, SpotCalendar, SpotLag).ToOADate());

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();

            var lastBumpIx = _pillarDates.Length;

            if(LastDateToBump.HasValue)
            {
                var ix = Array.BinarySearch(_pillarDates, LastDateToBump.Value);
                ix = (ix < 0) ? ~ix : ix;
                ix+=2;
                lastBumpIx = System.Math.Min(ix, lastBumpIx); //cap at last pillar
            }

            for (var i = 0; i < lastBumpIx; i++)
            {
                var bumpedCurve = _prices.Select((x, ix) => ix == i ? x + bumpSize : x).ToArray();
                var c = new PriceCurve(BuildDate, _pillarDates, bumpedCurve, _curveType, _currencyProvider, _pillarLabels)
                {
                    CollateralSpec = CollateralSpec,
                    Currency = Currency,
                    AssetId = AssetId,
                    SpotCalendar = SpotCalendar,
                    SpotLag = SpotLag
                };
                var name = _pillarLabels[i];
                o.Add(name, c);
            }
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            switch (_curveType)
            {
                case PriceCurveType.Linear:
                    var oldSpotDate = BuildDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                    var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                    var newPillars = _pillarDates
                        .Where(d => d >= newAnchorDate && d != oldSpotDate)
                        .Concat(new[] { newSpotDate })
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var newPrices = newPillars.Select(x => GetPriceForDate(x)).ToArray();
                    return new PriceCurve(newAnchorDate, newPillars, newPrices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                case PriceCurveType.NYMEX:
                    var newPillarsNM = _pillarDates
                        .Where(d => d >= newAnchorDate)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var newPricesNM = newPillarsNM.Select(x => GetPriceForDate(x)).ToArray();
                    return new PriceCurve(newAnchorDate, newPillarsNM, newPricesNM, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                case PriceCurveType.ICE:
                    var newPillarsIC = _pillarDates
                        .Where(d => d > newAnchorDate)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var newPricesIC = newPillarsIC.Select(x => GetPriceForDate(x.AddDays(-1))).ToArray();
                    return new PriceCurve(newAnchorDate, newPillarsIC, newPricesIC, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                case PriceCurveType.Constant:
                    return new ConstantPriceCurve(_prices.First(), BuildDate, _currencyProvider);
                default:
                    throw new Exception("Unknown curve type");
            }
        }

        public DateTime PillarDatesForLabel(string label)
        {
            var labelIx = Array.IndexOf(_pillarLabels, label);
            return _pillarDates[labelIx];
        }
    }
}
