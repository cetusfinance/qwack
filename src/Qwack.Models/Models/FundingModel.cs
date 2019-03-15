using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Core.Models;

namespace Qwack.Models
{
    public class FundingModel : IFundingModel
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        private FundingModel()
        {
        }

        public FundingModel(DateTime buildDate, IrCurve[] curves, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            BuildDate = buildDate;
            Curves = new Dictionary<string, IrCurve>(curves.ToDictionary(kv => kv.Name, kv => kv));
            FxMatrix = new FxMatrix(_currencyProvider);
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        public FundingModel(DateTime buildDate, Dictionary<string, IrCurve> curves, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            BuildDate = buildDate;
            Curves = new Dictionary<string, IrCurve>(curves);
            FxMatrix = new FxMatrix(_currencyProvider);
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        private void SetupMappings()
        {
            foreach(var curve in Curves)
            {
                var key = $"{curve.Value.Currency.Ccy}é{curve.Value.CollateralSpec}";
                if (_curvesBySpec.ContainsKey(key))
                    throw new Exception($"More than one curve specifed with collateral key {key}");
                _curvesBySpec.Add(key, curve.Key);
            }
        }

        public IrCurve GetCurveByCCyAndSpec(Currency ccy, string collateralSpec)
        {
            var key = $"{ccy.Ccy}é{collateralSpec}";
            if (!_curvesBySpec.TryGetValue(key, out var curveName))
                throw new Exception($"Could not find a curve with currency {ccy.Ccy} and collateral spec {collateralSpec}");

            return Curves[curveName];
        }

        public Dictionary<string, IrCurve> Curves { get; private set; }
        public Dictionary<string, IVolSurface> VolSurfaces { get; set; }

        private Dictionary<string, string> _curvesBySpec = new Dictionary<string, string>();

        public DateTime BuildDate { get; private set; }
        public IFxMatrix FxMatrix { get; private set; }
        public string CurrentSolveCurve { get; set; }

        public void UpdateCurves(Dictionary<string, IrCurve> updateCurves) => Curves = new Dictionary<string, IrCurve>(updateCurves);

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
                VolSurfaces = VolSurfaces
            };
            return returnValue;
        }

        public IFundingModel DeepClone(DateTime? newBuildDate = null)
        {
            var returnValue = new FundingModel(newBuildDate ?? BuildDate, Curves.Values.Select(c => c.Clone()).ToArray(), _currencyProvider, _calendarProvider)
            {
                VolSurfaces = VolSurfaces == null ? new Dictionary<string, IVolSurface>() : new Dictionary<string, IVolSurface>(VolSurfaces)
            };

            if (FxMatrix != null)
                returnValue.SetupFx(FxMatrix.Clone());
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
            var spotDate = BuildDate.AddPeriod(RollType.F, fxPair.SettlementCalendar, fxPair.SpotLag);
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
                var pair = FxMatrix.FxPairDefinitions.First(x => x.Domestic == domesticCcy && x.Foreign == foreignCcy);
                var settleDates = fixingDates.Select(x => x.AddPeriod(RollType.F, pair.SettlementCalendar, pair.SpotLag));
                var rates = settleDates.Select(d => GetFxRate(d, domesticCcy, foreignCcy)).ToArray();
                return rates;
            }
        }

        public void SetupFx(IFxMatrix fxMatrix) => FxMatrix = fxMatrix;

        public IrCurve GetCurve(string name) => Curves.TryGetValue(name, out var curve) ? curve : throw new Exception($"Curve named {name} not found");
        public IVolSurface GetVolSurface(string name) => VolSurfaces.TryGetValue(name, out var curve) ? curve : throw new Exception($"Surface named {name} not found");

        public static IFundingModel RemapBaseCurrency(IFundingModel input, Currency newBaseCurrency, ICurrencyProvider currencyProvider)
        {
            if (newBaseCurrency == input.FxMatrix.BaseCurrency)
                return input.Clone();

            var mf = input.DeepClone(null);   
            var homeToBase = mf.FxMatrix.SpotRates[newBaseCurrency];
            var ccys = mf.FxMatrix.SpotRates.Keys.ToList()
                .Concat(new[] { mf.FxMatrix.BaseCurrency })
                .Where(x => x != newBaseCurrency);
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
    }

    
}
