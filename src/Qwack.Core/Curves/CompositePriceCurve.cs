using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Models;
using Qwack.Core.Basic;
using Qwack.Core.Descriptors;

namespace Qwack.Core.Curves
{
    public class CompositePriceCurve : IPriceCurve
    {
        public DateTime BuildDate { get; private set; }
        public Currency DomesticCurrency { get; }
        public Currency ForeignCurrency { get; }
        public string Name { get; set; }

        public string AssetId => pCurveFunc.Invoke().AssetId;

        public bool UnderlyingsAreForwards => pCurveFunc.Invoke().UnderlyingsAreForwards;
        public DateTime[] PillarDates => pCurveFunc.Invoke().PillarDates;

        public int NumberOfPillars => 0;

        public Currency Currency { get; set; } = new Currency("USD", DayCountBasis.ACT360, null);

        private Func<IFundingModel> fModelFunc;
        private Func<IPriceCurve> pCurveFunc;

        public List<MarketDataDescriptor> Descriptors => new List<MarketDataDescriptor>()
            {
                    new AssetCurveDescriptor {
                        AssetId =AssetId,
                        Currency =Currency,
                        Name =Name,
                        ValDate =BuildDate}
            };
        public List<MarketDataDescriptor> Dependencies => new List<MarketDataDescriptor>();


        public CompositePriceCurve(DateTime buildDate, Func<IPriceCurve> priceCurve, Func<IFundingModel> fundingModel, Currency domesticCurrency, Currency foreignCurrency)
        {
            BuildDate = buildDate;
            fModelFunc = fundingModel;
            pCurveFunc = priceCurve;
            DomesticCurrency = domesticCurrency;
            ForeignCurrency = foreignCurrency;
        }

        IFundingModel _fModel;
        IPriceCurve _pCurve;
        public CompositePriceCurve(DateTime buildDate, IPriceCurve priceCurve, IFundingModel fundingModel, Currency domesticCurrency, Currency foreignCurrency)
        {
            BuildDate = buildDate;
            _fModel = fundingModel;
            _pCurve = priceCurve;
            fModelFunc = new Func<IFundingModel>(() => { return _fModel; });
            pCurveFunc = new Func<IPriceCurve>(() => { return _pCurve; });
            DomesticCurrency = domesticCurrency;
            ForeignCurrency = foreignCurrency;
        }

        public double GetAveragePriceForDates(DateTime[] dates)
        {
            var model = fModelFunc.Invoke();
            var curve = pCurveFunc.Invoke();
            return dates.Select(date =>
            model.GetFxRate(date, DomesticCurrency, ForeignCurrency) * curve.GetPriceForDate(date)
            ).Average();
        }

        public double GetPriceForDate(DateTime date)
        {
            var model = fModelFunc.Invoke();
            var curve = pCurveFunc.Invoke();
            var fxfwd = model.GetFxRate(date, DomesticCurrency, ForeignCurrency);
            var commofwd = curve.GetPriceForDate(date);
            return commofwd * fxfwd;
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize)
        {
            var o = new Dictionary<string, IPriceCurve>();
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate) => new CompositePriceCurve(newAnchorDate, pCurveFunc.Invoke().RebaseDate(newAnchorDate), fModelFunc.Invoke(), DomesticCurrency, ForeignCurrency);

        public DateTime PillarDatesForLabel(string label)
        {
            throw new NotImplementedException();
        }
    }
}
