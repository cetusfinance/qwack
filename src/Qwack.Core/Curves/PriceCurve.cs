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

        public Currency Currency { get; set; }
        public string CollateralSpec { get; set; }

        public List<MarketDataDescriptor> Descriptors => new List<MarketDataDescriptor>()
            {
                    new AssetCurveDescriptor {
                        AssetId =AssetId,
                        Currency =Currency,
                        Name =Name,
                        ValDate =BuildDate}
            };
        public List<MarketDataDescriptor> Dependencies => new List<MarketDataDescriptor>();

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
                    pillarsAsDoubles.Select(x => x - 1).ToArray();
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.NextValue);
                    break;
                default:
                    throw new Exception($"Unkown price curve type {_curveType}");
            }
        }

        public double GetAveragePriceForDates(DateTime[] dates) => _interp.Average(dates.Select(x => x.ToOADate()));

        public double GetPriceForDate(DateTime date) => _interp.Interpolate(date.ToOADate());

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
                    AssetId = AssetId
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
                    var todaySpotDate = BuildDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar); //this should use currency calendar!
                    if (_pillarDates.First() <= todaySpotDate)
                    {
                        var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                        var newSpot = GetPriceForDate(newSpotDate);
                        var newPillars = ((DateTime[])_pillarDates.Clone()).ToList();
                        newPillars[0] = newSpotDate;
                        var newPrices = ((double[])_prices.Clone()).ToList();
                        newPrices[0] = newSpot;
                        if(newPillars[0]==newPillars[1])
                        {
                            newPillars.RemoveAt(0);
                            newPrices.RemoveAt(0);
                        }
                        return new PriceCurve(newAnchorDate, newPillars.ToArray(), newPrices.ToArray(), _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                    }
                    else
                        return new PriceCurve(newAnchorDate, _pillarDates, _prices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                case PriceCurveType.NYMEX:
                    if (_pillarDates.First() < newAnchorDate) //remove first point as it has expired tomorrow
                    {
                        var newPillars = ((DateTime[])_pillarDates.Clone()).ToList();
                        newPillars.RemoveAt(0);
                        var newPrices = ((double[])_prices.Clone()).ToList();
                        newPrices.RemoveAt(0);
                        return new PriceCurve(newAnchorDate, newPillars.ToArray(), newPrices.ToArray(), _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                    }
                    else
                    {
                        return new PriceCurve(newAnchorDate, _pillarDates, _prices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                    }
                case PriceCurveType.ICE:
                    if (_pillarDates.First() <= newAnchorDate) //difference to NYMEX case is "<=" vs "<"
                    {
                        var newPillars = ((DateTime[])_pillarDates.Clone()).ToList();
                        newPillars.RemoveAt(0);
                        var newPrices = ((double[])_prices.Clone()).ToList();
                        newPrices.RemoveAt(0);
                        return new PriceCurve(newAnchorDate, newPillars.ToArray(), newPrices.ToArray(), _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                    }
                    else
                    {
                        return new PriceCurve(newAnchorDate, _pillarDates, _prices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId };
                    }
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
