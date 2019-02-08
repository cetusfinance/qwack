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
    public class FxForwardCurve : IPriceCurve
    {
        public DateTime BuildDate { get; private set; }
        public Currency DomesticCurrency { get; }
        public Currency ForeignCurrency { get; }
        public string Name { get; set; }

        public int NumberOfPillars => 0;

        public PriceCurveType CurveType => PriceCurveType.Linear;

        public Currency Currency { get => ForeignCurrency; set => throw new Exception(); }

        public bool UnderlyingsAreForwards => true;

        private Func<IFundingModel> fModelFunc;

        public DateTime[] PillarDates => null;

        public List<MarketDataDescriptor> Descriptors => new List<MarketDataDescriptor>()
            {
                    new AssetCurveDescriptor {
                        AssetId = ForeignCurrency.Ccy,
                        Currency =Currency,
                        Name =Name,
                        ValDate =BuildDate}
            };
        public List<MarketDataDescriptor> Dependencies => new List<MarketDataDescriptor>();
        public Dictionary<MarketDataDescriptor, object> DependentReferences => new Dictionary<MarketDataDescriptor, object>();

        public string AssetId => ForeignCurrency.Ccy;

        public Frequency SpotLag { get; set; } = new Frequency("0b");
        public Calendar SpotCalendar { get; set; }

        public FxForwardCurve(DateTime buildDate, Func<IFundingModel> fundingModel, Currency domesticCurrency, Currency foreignCurrency)
        {
            BuildDate = buildDate;
            fModelFunc = fundingModel;
            DomesticCurrency = domesticCurrency;
            ForeignCurrency = foreignCurrency;
        }

        IFundingModel _fModel;
        public FxForwardCurve(DateTime buildDate, IFundingModel fundingModel, Currency domesticCurrency, Currency foreignCurrency)
        {
            BuildDate = buildDate;
            _fModel = fundingModel;
            fModelFunc = new Func<IFundingModel>(() => { return _fModel; });
            DomesticCurrency = domesticCurrency;
            ForeignCurrency = foreignCurrency;
        }

        public double GetAveragePriceForDates(DateTime[] dates)
        {
            var model = fModelFunc.Invoke();
            return dates.Select(date=>model.GetFxRate(date, DomesticCurrency, ForeignCurrency)).Average();
        }

        public double GetPriceForDate(DateTime date)
        {
            var model = fModelFunc.Invoke();
            return model.GetFxRate(date, DomesticCurrency, ForeignCurrency);
        }

        public double GetPriceForFixingDate(DateTime date)
        {
            var model = fModelFunc.Invoke();
            var pair = model.FxMatrix.GetFxPair(DomesticCurrency, ForeignCurrency);
            return GetPriceForDate(pair.SpotDate(date));
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate) => throw new NotImplementedException();

        public DateTime PillarDatesForLabel(string label) => throw new NotImplementedException();
    }
}
