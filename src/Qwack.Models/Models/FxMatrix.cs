using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Models;

namespace Qwack.Models
{
    public class FxMatrix : IFxMatrix
    {
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

        public FxPair GetFxPair(Currency domesticCcy, Currency foreignCcy) => FxPairDefinitions.SingleOrDefault(x => x.Domestic == domesticCcy && x.Foreign == foreignCcy);

        public IFxMatrix Clone()
        {
            var o = new FxMatrix();
            o.Init(BaseCurrency, BuildDate, new Dictionary<Currency, double>(SpotRates), new List<FxPair>(FxPairDefinitions), new Dictionary<Currency, string>(DiscountCurveMap));
            return o;
        }
    }
}
