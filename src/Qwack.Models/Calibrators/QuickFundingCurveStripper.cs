using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;

namespace Qwack.Models.Calibrators
{
    public static class QuickFundingCurveStripper
    {
        public static IrCurve StripFlatSpread(string name, double spread, FloatRateIndex floatRateIndex, IrCurve baseCurve, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var pillars = baseCurve.PillarDates;
            var startRates = baseCurve.GetRates().Select(x => x + spread).ToArray();
            var fCurve = new IrCurve(pillars, startRates, baseCurve.BuildDate, name, baseCurve.InterpolatorType, baseCurve.Currency, "FUNDING", baseCurve.RateStorageType)
            {
                SolveStage = 0
            };
            var instruments = pillars.Select(p =>
                new FloatingRateLoanDepo(baseCurve.BuildDate, p, floatRateIndex, 1e6, spread, baseCurve.Name, name)  //-spread hack
                { SolveCurve = name, PillarDate = p }).ToArray();
            var fic = new FundingInstrumentCollection(currencyProvider);
            fic.AddRange(instruments);
            var solver = new NewtonRaphsonMultiCurveSolverStaged();
            var fm = new FundingModel(baseCurve.BuildDate, new[] { baseCurve, fCurve }, currencyProvider, calendarProvider);
            solver.Solve(fm, fic);

            return fCurve;
        }
    }
}
