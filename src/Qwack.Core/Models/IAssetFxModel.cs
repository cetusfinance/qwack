using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;

namespace Qwack.Core.Models
{
    public interface IAssetFxModel : IPvModel
    {
        DateTime BuildDate { get; }
        IFundingModel FundingModel { get; }

        ICorrelationMatrix CorrelationMatrix { get; set; }

        void AddPriceCurve(string name, IPriceCurve curve);
        void AddVolSurface(string name, IVolSurface surface);
        void AddFixingDictionary(string name, IFixingDictionary fixings);

        void RemovePriceCurve(IPriceCurve curve);
        void RemoveVolSurface(IVolSurface surface);
        void RemoveFixingDictionary(string name);


        void AddPriceCurves(Dictionary<string, IPriceCurve> curves);
        void AddVolSurfaces(Dictionary<string, IVolSurface> surfaces);
        void AddFixingDictionaries(Dictionary<string, IFixingDictionary> fixings);

        double GetVolForStrikeAndDate(string name, DateTime expiry, double strike);
        double GetVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike);
        double GetAverageVolForStrikeAndDates(string name, DateTime[] expiries, double strike);
        double GetAverageVolForMoneynessAndDates(string name, DateTime[] expiries, double moneyness);
        double GetFxVolForStrikeAndDate(string name, DateTime expiry, double strike);
        double GetFxVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike);
        double GetCompositeVolForStrikeAndDate(string assetId, DateTime expiry, double strike, Currency ccy);
        IPriceCurve GetPriceCurve(string name, Currency ccy = null);
        IVolSurface GetVolSurface(string name, Currency ccy = null);
        bool TryGetVolSurface(string name, out IVolSurface surface, Currency currency = null);
        IFixingDictionary GetFixingDictionary(string name);
        bool TryGetFixingDictionary(string name, out IFixingDictionary fixings);

        IAssetFxModel Clone();
        IAssetFxModel Clone(IFundingModel fundingModel);
        IAssetFxModel TrimModel(Portfolio portfolio, string[] additionalIrCurves = null, string[] additionalCcys = null);
        string[] CurveNames { get; }
        string[] VolSurfaceNames { get; }
        string[] FixingDictionaryNames { get; }

        IPriceCurve[] Curves { get; }

        void AttachPortfolio(Portfolio portfolio);

        void BuildDependencyTree();
        string[] GetDependentCurves(string curve);
        string[] GetAllDependentCurves(string curve);

        void OverrideBuildDate(DateTime buildDate);

        double GetCorrelation(string label1, string label2, double t = 0);
    }
}
