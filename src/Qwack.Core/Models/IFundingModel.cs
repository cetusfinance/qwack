using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public interface IFundingModel
    {
        DateTime BuildDate { get; }
        string CurrentSolveCurve { get; set; }
        Dictionary<string, IrCurve> Curves { get; }
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
        void UpdateCurves(Dictionary<string, IrCurve> updateCurves);

        IrCurve GetCurveByCCyAndSpec(Currency ccy, string collateralSpec);
        IrCurve GetCurve(string name);
        IVolSurface GetVolSurface(string name);

        double GetDf(string curveName, DateTime startDate, DateTime endDate);
        double GetDf(Currency ccy, DateTime startDate, DateTime endDate);

        Currency GetCurrency(string currency);
    }
}
