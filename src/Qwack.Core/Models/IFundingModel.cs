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
        IFxMatrix FxMatrix { get; }

        IFundingModel BumpCurve(string curveName, int pillarIx, double deltaBump, bool mutate);
        IFundingModel Clone();
        IFundingModel DeepClone();
        double GetFxRate(DateTime settlementDate, Currency domesticCcy, Currency foreignCcy);
        void SetupFx(IFxMatrix fxMatrix);
        void UpdateCurves(Dictionary<string, IrCurve> updateCurves);
    }
}
