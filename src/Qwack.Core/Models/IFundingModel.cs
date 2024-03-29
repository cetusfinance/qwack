using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Core.Models
{
    public interface IFundingModel
    {
        DateTime BuildDate { get; }
        string CurrentSolveCurve { get; set; }
        Dictionary<string, IIrCurve> Curves { get; }
        Dictionary<string, IVolSurface> VolSurfaces { get; set; }
        IFxMatrix FxMatrix { get; }

        IFundingModel BumpCurve(string curveName, int pillarIx, double deltaBump, bool mutate);
        IFundingModel Clone();
        IFundingModel DeepClone(DateTime? newBuildDate);

        double GetFxRate(DateTime settlementDate, Currency domesticCcy, Currency foreignCcy);
        double GetFxRate(DateTime settlementDate, string fxPair);
        double[] GetFxRates(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy);
        double GetFxAverage(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy);
        void SetupFx(IFxMatrix fxMatrix);
        void UpdateCurves(Dictionary<string, IIrCurve> updateCurves);

        IIrCurve GetCurveByCCyAndSpec(Currency ccy, string collateralSpec);
        IIrCurve GetCurve(string name);
        IVolSurface GetVolSurface(string name);
        bool TryGetVolSurface(string name, out IVolSurface volSurface);
        double GetDf(string curveName, DateTime startDate, DateTime endDate);
        double GetDf(Currency ccy, DateTime startDate, DateTime endDate);

        Currency GetCurrency(string currency);

        double CalibrationTimeMs { get; set; }
        Dictionary<int, int> CalibrationItterations { get; set; }
        Dictionary<int, string> CalibrationCurves { get; set; }

        TO_FundingModel GetTransportObject();

        IFixingDictionary GetFixingDictionary(string name);
        bool TryGetFixingDictionary(string name, out IFixingDictionary fixings);
        void AddFixingDictionary(string name, IFixingDictionary fixings);
        void RemoveFixingDictionary(string name);
    }
}
