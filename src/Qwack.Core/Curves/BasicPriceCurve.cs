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
    public class BasicPriceCurve : IPriceCurve
    {
        private readonly DateTime[] _pillarDates = Array.Empty<DateTime>();
        private readonly double[] _prices;
        private readonly PriceCurveType _curveType;
        private IInterpolator1D _interp;
        private readonly ICurrencyProvider _currencyProvider;

        public bool UnderlyingsAreForwards => _curveType == PriceCurveType.LME;

        public PriceCurveType CurveType => _curveType;

        public CommodityUnits Units { get; set; }

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

        public BasicPriceCurve(DateTime buildDate, DateTime[] PillarDates, double[] Prices, PriceCurveType curveType, ICurrencyProvider currencyProvider, string[] pillarLabels = null)
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

        public BasicPriceCurve(TO_BasicPriceCurve transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) :
            this(transportObject.BuildDate, transportObject.PillarDates, transportObject.Prices, transportObject.CurveType, currencyProvider, transportObject.PillarLabels)
        {
            Currency = currencyProvider.GetCurrency(transportObject.Currency);
            CollateralSpec = transportObject.CollateralSpec;
            Name = transportObject.Name;
            AssetId = transportObject.AssetId;
            Units = transportObject.Units;
            SpotCalendar = calendarProvider.GetCalendarSafe(transportObject.SpotCalendar);
            SpotLag = string.IsNullOrEmpty(transportObject.SpotLag) ? 0.Bd() : new Frequency(transportObject.SpotLag);  
        }

        private void Initialize()
        {
            var pillarsAsDoubles = _pillarDates.Select(x => x.ToOADate()).ToArray();
            switch (_curveType)
            {
                case PriceCurveType.Linear:
                    _interp = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, _prices, Interpolator1DType.Linear, isSorted: true);
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

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump, DateTime[] sparsePointsToBump = null)
        {
            var o = new Dictionary<string, IPriceCurve>();

            if (sparsePointsToBump != null)
            {
                var pricesDict = sparsePointsToBump.ToDictionary(x => x, GetPriceForDate);
                var pricesSparse = _pillarDates.Select(x =>(double?)(pricesDict.TryGetValue(x, out var y) ? y : null)).ToArray();
                for (var i = 0; i < sparsePointsToBump.Length; i++)
                {
                    var curvePointIx = Array.IndexOf(_pillarDates, sparsePointsToBump[i]);

                    if (curvePointIx < 0)
                        continue;

                    var bumpedCurveSparse = pricesSparse.Select((x, ix) => (ix == curvePointIx ? x + bumpSize : x) as double?).ToArray();
                    var bumpedCurveBent = CurveBender.Bend(_prices, bumpedCurveSparse);
                    var c = new BasicPriceCurve(BuildDate, _pillarDates, bumpedCurveBent, _curveType, _currencyProvider, _pillarLabels)
                    {
                        CollateralSpec = CollateralSpec,
                        Currency = Currency,
                        AssetId = AssetId,
                        SpotCalendar = SpotCalendar,
                        SpotLag = SpotLag
                    };
         
                    var name = _pillarLabels[curvePointIx];
                    o.Add(name, c);
                }
            }
            else
            {
                var lastBumpIx = _pillarDates.Length;

                if (LastDateToBump.HasValue)
                {
                    var ix = Array.BinarySearch(_pillarDates, LastDateToBump.Value);
                    ix = (ix < 0) ? ~ix : ix;
                    ix += 2;
                    lastBumpIx = System.Math.Min(ix, lastBumpIx); //cap at last pillar
                }

                for (var i = 0; i < lastBumpIx; i++)
                {
                    var bumpedCurve = _prices.Select((x, ix) => ix == i ? x + bumpSize : x).ToArray();
                    var c = new BasicPriceCurve(BuildDate, _pillarDates, bumpedCurve, _curveType, _currencyProvider, _pillarLabels)
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
            }

            return o;
        }

        public IPriceCurve Clone() => new BasicPriceCurve(BuildDate, _pillarDates.ToArray(), _prices.ToArray(), _curveType, _currencyProvider, _pillarLabels.ToArray())
        {
            CollateralSpec = CollateralSpec,
            Currency = Currency,
            AssetId = AssetId,
            SpotCalendar = SpotCalendar,
            SpotLag = SpotLag,
            Name = Name
        };

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            switch (_curveType)
            {
                case PriceCurveType.Linear:
                    var oldSpotDate = BuildDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                    if (_pillarDates.Contains(oldSpotDate))
                    {
                        var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                        var newPillars = _pillarDates
                            .Where(d => d >= newAnchorDate && d != oldSpotDate)
                            .Concat(new[] { newSpotDate })
                            .Distinct()
                            .OrderBy(x => x)
                            .ToArray();
                        var newPrices = newPillars.Select(x => GetPriceForDate(x)).ToArray();
                        return new BasicPriceCurve(newAnchorDate, newPillars, newPrices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                    }
                    else
                    {
                        var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
                        var newPillars = _pillarDates
                            .Where(d => d >= newAnchorDate && d != oldSpotDate)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToArray();
                        var newPrices = newPillars.Select(x => GetPriceForDate(x)).ToArray();
                        return new BasicPriceCurve(newAnchorDate, newPillars, newPrices, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                    }
                case PriceCurveType.NYMEX:
                    var newPillarsNM = _pillarDates
                        .Where(d => d >= newAnchorDate)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var newPricesNM = newPillarsNM.Select(x => GetPriceForDate(x)).ToArray();
                    return new BasicPriceCurve(newAnchorDate, newPillarsNM, newPricesNM, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                case PriceCurveType.ICE:
                    var newPillarsIC = _pillarDates
                        .Where(d => d > newAnchorDate)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToArray();
                    var newPricesIC = newPillarsIC.Select(x => GetPriceForDate(x.AddDays(-1))).ToArray();
                    return new BasicPriceCurve(newAnchorDate, newPillarsIC, newPricesIC, _curveType, _currencyProvider, _pillarLabels) { CollateralSpec = CollateralSpec, Currency = Currency, AssetId = AssetId, SpotCalendar = SpotCalendar, SpotLag = SpotLag };
                case PriceCurveType.Constant:
                    return new ConstantPriceCurve(_prices.First(), BuildDate, _currencyProvider);
                default:
                    throw new Exception("Unknown curve type");
            }
        }

        public DateTime PillarDatesForLabel(string label)
        {
            var labelIx = Array.IndexOf(_pillarLabels, label);
            if (labelIx < 0)
                throw new Exception($"Could not find pillar matching label {label}");
            return _pillarDates[labelIx];
        }

        public TO_BasicPriceCurve ToTransportObject() =>
            new()
            {
                AssetId = AssetId,
                BuildDate = BuildDate,
                CollateralSpec = CollateralSpec,
                Currency = Currency?.Ccy,
                CurveType = CurveType,
                Name = Name,
                PillarDates = PillarDates,
                PillarLabels = PillarLabels,
                Prices = Prices,
                SpotCalendar = SpotCalendar?.Name,
                SpotLag = SpotLag.ToString()
            };
    }
}
