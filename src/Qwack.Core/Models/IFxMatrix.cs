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
        double GetSpotRate(Currency ccy);

        FxPair GetFxPair(Currency domesticCcy, Currency foreignCcy);
        FxPair GetFxPair(string pair);

        void Init(Currency baseCurrency, DateTime buildDate, Dictionary<Currency, double> spotRates, List<FxPair> fXPairDefinitions, Dictionary<Currency, string> discountCurveMap);
        void UpdateSpotRates(Dictionary<Currency, double> spotRates);

        IFxMatrix Clone();
        IFxMatrix Rebase(DateTime newBuildDate, Dictionary<Currency, double> newSpotRates);
    }
}
