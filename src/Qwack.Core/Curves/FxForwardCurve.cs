using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class FxForwardCurve : IPriceCurve
    {
        private readonly IFundingModel _fModel;
        private readonly Func<IFundingModel> fModelFunc;

        public DateTime BuildDate { get; private set; }
        public Currency DomesticCurrency { get; }
        public Currency ForeignCurrency { get; }
        public string Name { get; set; }
        public CommodityUnits Units { get; set; }
        public DateTime RefDate { get; set; }

        public int NumberOfPillars => 0;
        public PriceCurveType CurveType => PriceCurveType.Linear;
        public Currency Currency { get => ForeignCurrency; set => throw new Exception(); }
        public bool UnderlyingsAreForwards => true;
        public DateTime[] PillarDates => null;
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
            return dates.Select(date => model.GetFxRate(date, DomesticCurrency, ForeignCurrency)).Average();
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

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump, DateTime[] sparsePointsToBump = null)
        {
            var o = new Dictionary<string, IPriceCurve>();
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate) => throw new NotImplementedException();

        public DateTime PillarDatesForLabel(string label) => throw new NotImplementedException();
    }
}
