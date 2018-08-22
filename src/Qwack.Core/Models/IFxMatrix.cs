using System;
using System.Collections.Generic;
using Qwack.Core.Basic;

namespace Qwack.Core.Models
{
    public interface IFxMatrix
    {
        Currency BaseCurrency { get; }
        DateTime BuildDate { get; }
        Dictionary<Currency, string> DiscountCurveMap { get; }
        List<FxPair> FxPairDefinitions { get; }
        Dictionary<Currency, double> SpotRates { get; }

        FxPair GetFxPair(Currency domesticCcy, Currency foreignCcy);
        void Init(Currency baseCurrency, DateTime buildDate, Dictionary<Currency, double> spotRates, List<FxPair> fXPairDefinitions, Dictionary<Currency, string> discountCurveMap);
        void UpdateSpotRates(Dictionary<Currency, double> spotRates);
    }
}
