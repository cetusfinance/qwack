using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Models
{
    public class FundingModel : IFundingModel
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        private FundingModel()
        {
        }

        public FundingModel(DateTime buildDate, IIrCurve[] curves, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            BuildDate = buildDate;
            Curves = new Dictionary<string, IIrCurve>(curves.ToDictionary(kv => kv.Name, kv => kv));
            FxMatrix = new FxMatrix(_currencyProvider);
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        public FundingModel(DateTime buildDate, Dictionary<string, IIrCurve> curves, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            BuildDate = buildDate;
            Curves = new Dictionary<string, IIrCurve>(curves);
            FxMatrix = new FxMatrix(_currencyProvider);
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        public FundingModel(TO_FundingModel transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) :
            this(transportObject.BuildDate, transportObject.Curves.ToDictionary(x => x.Key, x =>
            {
                if (x.Value.IrCurve != null)
                    return (IIrCurve)new IrCurve(x.Value.IrCurve, currencyProvider);
                else if (x.Value.SeasonalCpiCurve != null)
                    return (IIrCurve)new SeasonalCpiCurve(x.Value.SeasonalCpiCurve, calendarProvider);
                else
                    return (IIrCurve)new CPICurve(x.Value.CPICurve, calendarProvider);
            }), currencyProvider, calendarProvider)
        {
            if (transportObject.VolSurfaces != null)
                VolSurfaces = transportObject.VolSurfaces.ToDictionary(x => x.Key, x => x.Value.GetVolSurface(currencyProvider));
            _fixings = transportObject.Fixings?.ToDictionary(x => x.Key, x => (IFixingDictionary)new FixingDictionary(x.Value)) ?? new Dictionary<string, IFixingDictionary>();
            SetupFx(new FxMatrix(transportObject.FxMatrix, currencyProvider, calendarProvider));
        }


        private void SetupMappings()
        {
            foreach (var curve in Curves)
            {
                if (curve.Value is IrCurve c)
                {
                    var key = $"{c.Currency.Ccy}é{c.CollateralSpec}";
                    if (_curvesBySpec.ContainsKey(key))
                        throw new Exception($"More than one curve specifed with collateral key {key}");
                    _curvesBySpec.Add(key, curve.Key);
                }
            }
        }

        public IIrCurve GetCurveByCCyAndSpec(Currency ccy, string collateralSpec)
        {
            var key = $"{ccy.Ccy}é{collateralSpec}";
            if (!_curvesBySpec.TryGetValue(key, out var curveName))
            {
                foreach (var kv in _curvesBySpec)
                {
                    if (kv.Key.StartsWith(ccy.Ccy))
                        return Curves[kv.Value];
                }
                throw new Exception($"Could not find a curve with currency {ccy.Ccy} and collateral spec {collateralSpec}");
            }


            return Curves[curveName];
        }

        public Dictionary<string, IIrCurve> Curves { get; private set; }
        public Dictionary<string, IVolSurface> VolSurfaces { get; set; }

        private readonly Dictionary<string, string> _curvesBySpec = new();

        public DateTime BuildDate { get; private set; }
        public IFxMatrix FxMatrix { get; private set; }
        public string CurrentSolveCurve { get; set; }

        public double CalibrationTimeMs { get; set; }

        public Dictionary<int, int> CalibrationItterations { get; set; }
        public Dictionary<int, string> CalibrationCurves { get; set; }
        public void UpdateCurves(Dictionary<string, IIrCurve> updateCurves) => Curves = new Dictionary<string, IIrCurve>(updateCurves);

        private readonly Dictionary<string, IFixingDictionary> _fixings = new();

        public void AddFixingDictionary(string name, IFixingDictionary fixings) => _fixings[name] = fixings;
        public void AddFixingDictionaries(Dictionary<string, IFixingDictionary> fixings)
        {
            foreach (var kv in fixings)
                _fixings[kv.Key] = kv.Value;
        }
        public IFixingDictionary GetFixingDictionary(string name)
        {
            if (!_fixings.TryGetValue(name, out var dict))
                throw new Exception($"Fixing dictionary with name {name} not found");
            return dict;
        }
        public bool TryGetFixingDictionary(string name, out IFixingDictionary fixings) => _fixings.TryGetValue(name, out fixings);
        public void RemoveFixingDictionary(string name) => _fixings.Remove(name);

        public IFundingModel BumpCurve(string curveName, int pillarIx, double deltaBump, bool mutate)
        {
            var newModel = new FundingModel(BuildDate, Curves.Select(kv =>
            {
                if (kv.Key == curveName)
                {
                    return kv.Value.BumpRate(pillarIx, deltaBump, mutate);
                }
                else
                {
                    return kv.Value;
                }
            }).ToArray(), _currencyProvider, _calendarProvider)
            {
                FxMatrix = FxMatrix
            };
            return newModel;
        }

        public IFundingModel Clone()
        {
            var returnValue = new FundingModel(BuildDate, Curves.Values.ToArray(), _currencyProvider, _calendarProvider)
            {
                FxMatrix = FxMatrix,
                VolSurfaces = VolSurfaces,
            };

            foreach (var kv in _fixings)
                returnValue.AddFixingDictionary(kv.Key, kv.Value);

            return returnValue;
        }

        private static IIrCurve CloneCurve(IIrCurve curve)
        {
            if (curve is IrCurve ir)
                return ir.Clone();
            if (curve is SeasonalCpiCurve scpi)
                return scpi.Clone();
            if (curve is CPICurve cpi)
                return cpi.Clone();

            return (curve as IrCurve).Clone();
        }

        public IFundingModel DeepClone(DateTime? newBuildDate = null)
        {
            var returnValue = new FundingModel(newBuildDate ?? BuildDate, Curves.Values.Select(CloneCurve).ToArray(), _currencyProvider, _calendarProvider)
            {
                VolSurfaces = VolSurfaces == null ? new Dictionary<string, IVolSurface>() : new Dictionary<string, IVolSurface>(VolSurfaces)
            };

            if (FxMatrix != null)
                returnValue.SetupFx(FxMatrix.Clone());

            foreach (var kv in _fixings)
                returnValue.AddFixingDictionary(kv.Key, kv.Value);

            return returnValue;
        }

        public Currency GetCurrency(string currency) => _currencyProvider.GetCurrency(currency);

        public double GetFxRate(DateTime settlementDate, string fxPair)
        {
            var pair = fxPair.FxPairFromString(_currencyProvider, _calendarProvider);
            return GetFxRate(settlementDate, pair.Domestic, pair.Foreign);
        }

        public double GetFxRate(DateTime settlementDate, Currency domesticCcy, Currency foreignCcy)
        { //domestic-per-foreign
            if (foreignCcy == domesticCcy) return 1.0;

            double spot;

            if (domesticCcy == FxMatrix.BaseCurrency)
            {
                spot = FxMatrix.GetSpotRate(foreignCcy);
            }
            else if (foreignCcy == FxMatrix.BaseCurrency)
            {
                spot = 1.0 / FxMatrix.GetSpotRate(domesticCcy);
            }
            else
            {
                var forToBase = GetFxRate(settlementDate, FxMatrix.BaseCurrency, foreignCcy);
                var domToBase = GetFxRate(settlementDate, FxMatrix.BaseCurrency, domesticCcy);
                return forToBase / domToBase;
            }
            var fxPair = FxMatrix.GetFxPair(domesticCcy, foreignCcy);
            var spotDate = BuildDate.AddPeriod(RollType.F, fxPair.PrimaryCalendar, fxPair.SpotLag);
            var dfDom = GetDf(domesticCcy, spotDate, settlementDate);
            var dfFor = GetDf(foreignCcy, spotDate, settlementDate);

            return spot * dfDom / dfFor;
        }

        public double GetDf(string curveName, DateTime startDate, DateTime endDate)
        {
            if (!Curves.TryGetValue(curveName, out var curve))
                throw new Exception($"Curve with name {curveName} not found");

            return curve.GetDf(startDate, endDate);
        }

        public double GetDf(Currency ccy, DateTime startDate, DateTime endDate)
        {
            if (!FxMatrix.DiscountCurveMap.TryGetValue(ccy, out var curveName))
                throw new Exception($"Currency {ccy} not found in discounting map");

            if (!Curves.TryGetValue(curveName, out var curve))
                throw new Exception($"Curve with name {curveName} not found");

            return curve.GetDf(startDate, endDate);
        }

        public double GetFxAverage(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy) => GetFxRates(fixingDates, domesticCcy, foreignCcy).Average();

        public double[] GetFxRates(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy)
        {
            if (foreignCcy == domesticCcy)
                return Enumerable.Repeat(1.0, fixingDates.Length).ToArray();
            else
            {
                var pair = FxMatrix.GetFxPair(domesticCcy, foreignCcy);
                var settleDates = fixingDates.Select(x => x.AddPeriod(RollType.F, pair.PrimaryCalendar, pair.SpotLag));
                var rates = settleDates.Select(d => GetFxRate(d, domesticCcy, foreignCcy)).ToArray();
                return rates;
            }
        }

        public void SetupFx(IFxMatrix fxMatrix) => FxMatrix = fxMatrix;

        public IIrCurve GetCurve(string name) => Curves.TryGetValue(name, out var curve) ? curve : throw new Exception($"Curve named {name} not found");
        public IVolSurface GetVolSurface(string name) => TryGetVolSurface(name, out var surface) ? surface : throw new Exception($"Surface named {name} not found");
        private static string Shorten(string name) => name.Length == 7 && name[3] == '/' ? name.Substring(0, 3) + name.Substring(4, 3) : name;
        public bool TryGetVolSurface(string name, out IVolSurface volSurface)
        {
            if (VolSurfaces.TryGetValue(name, out volSurface))
                return true;

            if (VolSurfaces.TryGetValue(Shorten(name), out volSurface))
                return true;

            if (TryGetInverseSurface(name, out volSurface))
                return true;

            volSurface = null;
            return false;
        }
        private bool TryGetInverseSurface(string name, out IVolSurface volSurface)
        {
            var inverseName = name.Substring(name.Length - 3, 3) + (name.Length == 6 ? "" : "/") + name.Substring(0, 3);
            if (VolSurfaces.TryGetValue(inverseName, out volSurface))
                return true;
            if (VolSurfaces.TryGetValue(Shorten(inverseName), out volSurface))
                return true;

            return false;
        }

        public static IFundingModel RemapBaseCurrency(IFundingModel input, Currency newBaseCurrency, ICurrencyProvider currencyProvider)
        {
            if (newBaseCurrency == input.FxMatrix.BaseCurrency)
                return input.Clone();

            var mf = input.DeepClone(null);
            var homeToBase = mf.FxMatrix.SpotRates[newBaseCurrency];
            var ccys = mf.FxMatrix.SpotRates.Keys.ToList()
                .Concat(new[] { mf.FxMatrix.BaseCurrency })
                .Where(x => x != newBaseCurrency)
                .Distinct();
            var newRateDict = new Dictionary<Currency, double>();
            foreach (var ccy in ccys)
            {
                var spotDate = mf.FxMatrix.GetFxPair(newBaseCurrency, ccy).SpotDate(mf.BuildDate);
                var newRate = mf.GetFxRate(spotDate, newBaseCurrency, ccy);
                newRateDict.Add(ccy, newRate);
            }

            var newFx = new FxMatrix(currencyProvider);
            newFx.Init(newBaseCurrency, mf.FxMatrix.BuildDate, newRateDict, mf.FxMatrix.FxPairDefinitions, mf.FxMatrix.DiscountCurveMap);
            mf.SetupFx(newFx);

            return mf;
        }

        public TO_FundingModel GetTransportObject() =>
            new()
            {
                BuildDate = BuildDate,
                VolSurfaces = VolSurfaces.ToDictionary(x => x.Key, x => x.Value.GetTransportObject()),
                Curves = Curves.ToDictionary(x => x.Key, x =>
                {
                    if (x.Value is IrCurve ir)
                        return new TO_IIrCurve { IrCurve = ir.GetTransportObject() };
                    else if (x.Value is SeasonalCpiCurve scpi)
                        return new TO_IIrCurve { SeasonalCpiCurve = scpi.GetTransportObject() };
                    else if (x.Value is CPICurve cpi)
                        return new TO_IIrCurve { CPICurve = cpi.GetTransportObject() };
                    return null;
                }),
                FxMatrix = ((FxMatrix)FxMatrix).GetTransportObject(),
                Fixings = _fixings?.ToDictionary(x => x.Key, x => x.Value.GetTransportObject()),
            };
    }


}
