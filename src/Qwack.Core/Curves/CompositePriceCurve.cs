using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Models;
using Qwack.Core.Basic;

namespace Qwack.Core.Curves
{
    public class CompositePriceCurve : IPriceCurve
    {
        public DateTime BuildDate { get; private set; }
        public Currency CompoCurrency { get; }
        public Currency CurveCurrency => pCurveFunc.Invoke().Currency;

        public string Name { get => pCurveFunc.Invoke().Name; set => throw new Exception("Cant set name"); }

        public string AssetId => pCurveFunc.Invoke().AssetId;

        public bool UnderlyingsAreForwards => pCurveFunc.Invoke().UnderlyingsAreForwards;
        public DateTime[] PillarDates => pCurveFunc.Invoke().PillarDates;

        public Frequency SpotLag { get; set; } = new Frequency("0b");
        public Calendar SpotCalendar { get; set; }

        public int NumberOfPillars => pCurveFunc.Invoke().NumberOfPillars;

        public Currency Currency { get => CompoCurrency; set => throw new Exception("Cant set currency"); }

        private Func<IFundingModel> fModelFunc;
        private Func<IPriceCurve> pCurveFunc;

        public PriceCurveType CurveType => PriceCurveType.Linear;

        public CompositePriceCurve(DateTime buildDate, Func<IPriceCurve> priceCurve, Func<IFundingModel> fundingModel, Currency compoCurrency)
        {
            BuildDate = buildDate;
            fModelFunc = fundingModel;
            pCurveFunc = priceCurve;
            CompoCurrency = compoCurrency;
        }

        IFundingModel _fModel;
        IPriceCurve _pCurve;
        public CompositePriceCurve(DateTime buildDate, IPriceCurve priceCurve, IFundingModel fundingModel, Currency compoCurrency)
        {
            BuildDate = buildDate;
            _fModel = fundingModel;
            _pCurve = priceCurve;
            fModelFunc = new Func<IFundingModel>(() => { return _fModel; });
            pCurveFunc = new Func<IPriceCurve>(() => { return _pCurve; });
            CompoCurrency = compoCurrency;
        }

        public double GetAveragePriceForDates(DateTime[] dates)
        {
            var model = fModelFunc.Invoke();
            var curve = pCurveFunc.Invoke();
            return dates.Select(date =>
            model.GetFxRate(date, CurveCurrency, CompoCurrency) * curve.GetPriceForDate(date)
            ).Average();
        }

        public double GetPriceForDate(DateTime date)
        {
            var model = fModelFunc.Invoke();
            var curve = pCurveFunc.Invoke();
            var fxfwd = model.GetFxRate(date, CurveCurrency, CompoCurrency);
            var commofwd = curve.GetPriceForDate(date);
            return commofwd * fxfwd;
        }

        public double GetPriceForFixingDate(DateTime date)
        {
            var model = fModelFunc.Invoke();
            var curve = pCurveFunc.Invoke();
            var pair = model.FxMatrix.GetFxPair(CurveCurrency, CompoCurrency);
            var fxSpotDate = pair.SpotDate(date);
            var fxfwd = model.GetFxRate(fxSpotDate, CurveCurrency, CompoCurrency);
            var commofwd = curve.GetPriceForFixingDate(date);
            return commofwd * fxfwd;
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var b = pCurveFunc.Invoke().GetDeltaScenarios(bumpSize, LastDateToBump);

            var o = b.ToDictionary(k => k.Key, v => (IPriceCurve)new CompositePriceCurve(BuildDate, () => v.Value, fModelFunc, CompoCurrency));
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate) => new CompositePriceCurve(newAnchorDate, pCurveFunc.Invoke().RebaseDate(newAnchorDate), fModelFunc.Invoke(), CompoCurrency);

        public DateTime PillarDatesForLabel(string label) => pCurveFunc.Invoke().PillarDatesForLabel(label);
    }
}
