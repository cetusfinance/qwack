using System;
using System.Collections.Generic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;

namespace Qwack.Core.Models
{
    public interface INewtonRaphsonAssetBasisCurveSolver
    {
        int MaxItterations { get; set; }
        double Tollerance { get; set; }
        int UsedItterations { get; set; }

        PriceCurve SolveCurve(IEnumerable<IAssetInstrument> instruments, List<DateTime> pillars, IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, PriceCurveType curveType);
        SparsePriceCurve SolveSparseCurve(List<IAssetInstrument> instruments, List<DateTime> pillars, IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, SparsePriceCurveType curveType);
    }
}
