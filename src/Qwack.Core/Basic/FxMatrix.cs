using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Basic
{
    public class FxMatrix : IFxMatrix
    {
        private ICurrencyProvider _currencyProvider;

        public FxMatrix(ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
            SpotRates = new Dictionary<Currency, double>();
            DiscountCurveMap = new Dictionary<Currency, string>();
            FxPairDefinitions = new List<FxPair>();
        }

        public Currency BaseCurrency { get; private set; }

        public DateTime BuildDate { get; private set; }

        public List<FxPair> FxPairDefinitions { get; private set; }
        public Dictionary<Currency, string> DiscountCurveMap { get; private set; }
        public Dictionary<Currency, double> SpotRates { get; private set; }

        public void Init(Currency baseCurrency, DateTime buildDate, Dictionary<Currency, double> spotRates,
            List<FxPair> fXPairDefinitions, Dictionary<Currency, string> discountCurveMap)
        {
            BaseCurrency = baseCurrency;
            BuildDate = buildDate;
            SpotRates = spotRates;
            FxPairDefinitions = fXPairDefinitions;
            DiscountCurveMap = discountCurveMap;
        }

        public void UpdateSpotRates(Dictionary<Currency, double> spotRates) => SpotRates = spotRates;

        public FxPair GetFxPair(string pair)
        {
            var leftCCy = _currencyProvider[pair.Substring(0, 3)];
            var rightCcy = _currencyProvider[pair.Substring(pair.Length-3, 3)];
            return GetFxPair(leftCCy, rightCcy);
        }

        public double GetSpotRate(Currency ccy) => SpotRates.TryGetValue(ccy, out var spotRate) ? spotRate : throw new Exception($"Spot rate for currency {ccy.Ccy} not found");

        public bool TryGetSpotRate(Currency ccy, out double spotRate) => SpotRates.TryGetValue(ccy, out spotRate);


        public FxPair GetFxPair(Currency domesticCcy, Currency foreignCcy)
        {
            if(domesticCcy==foreignCcy)
                return new FxPair { Domestic = domesticCcy, Foreign = foreignCcy, SettlementCalendar = new Calendar(), SpotLag = 0.Day() };

            var pair = FxPairDefinitions.SingleOrDefault(x => x.Domestic == domesticCcy && x.Foreign == foreignCcy);
            return pair ?? 
                FxPairDefinitions.SingleOrDefault(x => x.Foreign == domesticCcy && x.Domestic == foreignCcy) ?? 
                new FxPair { Domestic = domesticCcy, Foreign = foreignCcy, SettlementCalendar = foreignCcy.SettlementCalendar.Merge(domesticCcy.SettlementCalendar), SpotLag = 2.Bd() };
        }

        public IFxMatrix Clone()
        {
            var o = new FxMatrix(_currencyProvider);
            o.Init(BaseCurrency, BuildDate, new Dictionary<Currency, double>(SpotRates), new List<FxPair>(FxPairDefinitions), new Dictionary<Currency, string>(DiscountCurveMap));
            return o;
        }

        public IFxMatrix Rebase(DateTime newBuildDate, Dictionary<Currency,double> newSpotRates)
        {
            var o = new FxMatrix(_currencyProvider);
            o.Init(BaseCurrency, newBuildDate, newSpotRates, new List<FxPair>(FxPairDefinitions), new Dictionary<Currency, string>(DiscountCurveMap));
            return o;
        }

        public string GetDiscountCurve(string currency) => DiscountCurveMap.TryGetValue(_currencyProvider.GetCurrency(currency), out var curve) ? curve : null;
    }
}
