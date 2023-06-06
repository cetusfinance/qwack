using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Models.Calibrators;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Providers.Json;
using Xunit;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Core.Tests.CurveSolving
{
    public class OisFact
    {
        bool IsCoverageOnly => bool.TryParse(Environment.GetEnvironmentVariable("CoverageOnly"), out var coverageOnly) && coverageOnly;

        private static readonly Currency ccyZar = TestProviderHelper.CurrencyProvider["JHB"];
        private static readonly Calendar _usd = TestProviderHelper.CalendarProvider.Collection["nyc"];
        private static readonly Calendar JHB = TestProviderHelper.CalendarProvider.Collection["JHB"];
        private static readonly Currency ccyUsd = TestProviderHelper.CurrencyProvider["USD"];
        private static readonly FloatRateIndex _zar3m = new FloatRateIndex()
        {
            Currency = ccyZar,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 3.Months(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = JHB,
            RollConvention = RollType.MF
        };
        private static readonly FloatRateIndex zaron = new FloatRateIndex()
        {
            Currency = ccyZar,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = JHB,
            RollConvention = RollType.F
        };
        private static readonly FloatRateIndex usdon = new FloatRateIndex()
        {
            Currency = ccyUsd,
            DayCountBasis = DayCountBasis.Act_365F,
            DayCountBasisFixed = DayCountBasis.Act_365F,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 1.Years(),
            FixingOffset = 0.Bd(),
            HolidayCalendars = _usd,
            RollConvention = RollType.F
        };
        private readonly FloatRateIndex usd3m = new FloatRateIndex()
        {
            Currency = ccyUsd,
            DayCountBasis = DayCountBasis.Act_360,
            DayCountBasisFixed = DayCountBasis.Act_360,
            ResetTenor = 3.Months(),
            ResetTenorFixed = 1.Years(),
            FixingOffset = 2.Bd(),
            HolidayCalendars = _usd,
            RollConvention = RollType.MF
        };

        [Fact]
        public void BasicOisCurveSolving()
        {
            var startDate = new DateTime(2016, 05, 20);
            var depoTenors = new Frequency[] { 3.Months() };
            double[] depoPrices = { 0.06 };
            string[] FRATenors = { "3x6", "6x9", "9x12" };
            double[] FRAPrices = { 0.065, 0.07, 0.075 };
            var swapTenors = new Frequency[] { 18.Months(), 2.Years(), 3.Years(), 4.Years(), 5.Years(), 7.Years(), 10.Years(), 15.Years(), 20.Years() };
            double[] swapPrices = { 0.075, 0.08, 0.085, 0.09, 0.095, 0.0975, 0.098, 0.099, 0.1 };
            var oisTenors = new Frequency[] { 3.Months(), 6.Months(), 1.Years(), 2.Years(), 3.Years(), 4.Years(), 5.Years(), 7.Years(), 10.Years(), 15.Years(), 20.Years() };
            var oisPrices = new double[] { 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004 };

            var pillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var pillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var pillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var pillarDates3m = pillarDatesDepo.Union(pillarDatesSwap).Union(pillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            var pillarDatesOIS = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();

            var swaps = new IrSwap[swapTenors.Length];
            var depos = new IrSwap[depoTenors.Length];
            var oisSwaps = new IrBasisSwap[oisTenors.Length];
            var FRAs = new ForwardRateAgreement[FRATenors.Length];

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            for (var i = 0; i < FRAs.Length; i++)
            {
                FRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPrices[i], _zar3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "ZAR.JIBAR.3M", "ZAR.OIS.1B");
                fic.Add(FRAs[i]);
            }

            for (var i = 0; i < oisSwaps.Length; i++)
            {
                oisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPrices[i], true, zaron, _zar3m, "ZAR.JIBAR.3M", "ZAR.OIS.1B", "ZAR.OIS.1B");
                fic.Add(oisSwaps[i]);
            }

            for (var i = 0; i < swaps.Length; i++)
            {
                swaps[i] = new IrSwap(startDate, swapTenors[i], _zar3m, swapPrices[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.OIS.1B");
                fic.Add(swaps[i]);
            }
            for (var i = 0; i < depos.Length; i++)
            {
                depos[i] = new IrSwap(startDate, depoTenors[i], _zar3m, depoPrices[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.OIS.1B");
                fic.Add(depos[i]);
            }

            var curve3m = new IrCurve(pillarDates3m, new double[pillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap, ccyZar);
            var curveOIS = new IrCurve(pillarDatesOIS, new double[pillarDatesOIS.Length], startDate, "ZAR.OIS.1B", Interpolator1DType.LinearFlatExtrap, ccyZar);
            var model = new FundingModel(startDate, new IrCurve[] { curve3m, curveOIS }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var S = new NewtonRaphsonMultiCurveSolver
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001,
                MaxItterations = IsCoverageOnly ? 1 : 100,
            };

            S.Solve(model, fic);

            if (!IsCoverageOnly)
            {
                foreach (var ins in fic)
                {
                    var pv = ins.Pv(model, false);
                    Assert.Equal(0.0, pv, 7);
                }
            }
        }

        [Fact]
        public void ComplexCurve()
        {
            var startDate = new DateTime(2016, 05, 20);
            var depoTenors = new Frequency[] { 3.Months() };
            var OISdepoTenors = new Frequency[] { 1.Bd() };
            double[] depoPricesZAR = { 0.06 };
            double[] depoPricesUSD = { 0.01 };
            double[] OISdepoPricesZAR = { 0.055 };
            double[] OISdepoPricesUSD = { 0.009 };

            string[] FRATenors = { "3x6", "6x9", "9x12", "12x15", "15x18", "18x21", "21x24" };
            double[] FRAPricesZAR = { 0.065, 0.07, 0.075, 0.077, 0.08, 0.081, 0.082 };
            double[] FRAPricesUSD = { 0.012, 0.013, 0.014, 0.015, 0.016, 0.017, 0.018 };

            Frequency[] swapTenors = { 3.Years(), 4.Years(), 5.Years(), 6.Years(), 7.Years(), 8.Years(), 9.Years(), 10.Years(), 12.Years(), 15.Years(), 20.Years(), 25.Years(), 30.Years() };
            double[] swapPricesZAR = { 0.08, 0.083, 0.085, 0.087, 0.089, 0.091, 0.092, 0.093, 0.094, 0.097, 0.099, 0.099, 0.099 };
            double[] swapPricesUSD = { 0.017, 0.018, 0.019, 0.020, 0.021, 0.022, 0.023, 0.024, 0.025, 0.026, 0.027, 0.028, 0.03 };
            Frequency[] oisTenors = { 3.Months(), 6.Months(), 1.Years(), 18.Months(), 2.Years(), 3.Years(), 4.Years(), 5.Years(), 6.Years(), 7.Years(), 8.Years(), 9.Years(), 10.Years(), 12.Years(), 15.Years(), 20.Years(), 25.Years(), 30.Years() };
            double[] oisPricesZAR = { 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004 };
            double[] oisPricesUSD = { 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002 };

            var ZARpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var ZARpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDates3m = ZARpillarDatesDepo.Union(ZARpillarDatesSwap).Union(ZARpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            var ZARpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesOIS = ZARpillarDatesDepoOIS.Union(ZARpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();


            var USDpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var USDpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDates3m = USDpillarDatesDepo.Union(USDpillarDatesSwap).Union(USDpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            var USDpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesOIS = USDpillarDatesDepoOIS.Union(USDpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();


            var ZARswaps = new IrSwap[swapTenors.Length];
            var ZARdepos = new IrSwap[depoTenors.Length];
            var ZARdeposOIS = new IrSwap[OISdepoTenors.Length];
            var ZARoisSwaps = new IrBasisSwap[oisTenors.Length];
            var ZARFRAs = new ForwardRateAgreement[FRATenors.Length];

            var USDswaps = new IrSwap[swapTenors.Length];
            var USDdepos = new IrSwap[depoTenors.Length];
            var USDdeposOIS = new IrSwap[OISdepoTenors.Length];
            var USDoisSwaps = new IrBasisSwap[oisTenors.Length];
            var USDFRAs = new ForwardRateAgreement[FRATenors.Length];


            var FIC = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            for (var i = 0; i < FRATenors.Length; i++)
            {
                ZARFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesZAR[i], _zar3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARFRAs[i]);
                USDFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesUSD[i], usd3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDFRAs[i]);
            }

            for (var i = 0; i < oisTenors.Length; i++)
            {
                ZARoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesZAR[i], true, zaron, _zar3m, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                FIC.Add(ZARoisSwaps[i]);
                USDoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesUSD[i], true, usdon, usd3m, "USD.LIBOR.3M", "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                FIC.Add(USDoisSwaps[i]);
            }

            for (var i = 0; i < swapTenors.Length; i++)
            {
                ZARswaps[i] = new IrSwap(startDate, swapTenors[i], _zar3m, swapPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARswaps[i]);
                USDswaps[i] = new IrSwap(startDate, swapTenors[i], usd3m, swapPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDswaps[i]);

            }

            for (var i = 0; i < depoTenors.Length; i++)
            {
                ZARdepos[i] = new IrSwap(startDate, depoTenors[i], _zar3m, depoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARdepos[i]);
                USDdepos[i] = new IrSwap(startDate, depoTenors[i], usd3m, depoPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDdepos[i]);
            }

            for (var i = 0; i < OISdepoTenors.Length; i++)
            {
                ZARdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], zaron, OISdepoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                FIC.Add(ZARdeposOIS[i]);
                USDdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], usdon, OISdepoPricesUSD[i], SwapPayReceiveType.Payer, "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                FIC.Add(USDdeposOIS[i]);
            }

            var ZARcurve3m = new IrCurve(ZARpillarDates3m, new double[ZARpillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap,ccyZar) { SolveStage = 0 };
            var ZARcurveOIS = new IrCurve(ZARpillarDatesOIS, new double[ZARpillarDatesOIS.Length], startDate, "ZAR.DISC.CSA_ZAR", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
            var USDcurve3m = new IrCurve(USDpillarDates3m, new double[USDpillarDates3m.Length], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };
            var USDcurveOIS = new IrCurve(USDpillarDatesOIS, new double[USDpillarDatesOIS.Length], startDate, "USD.DISC.CSA_USD", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };

            var engine = new FundingModel(startDate, new IrCurve[] { ZARcurve3m, ZARcurveOIS, USDcurve3m, USDcurveOIS }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var ZARcurve3m0 = new IrCurve(ZARpillarDates3m, new double[ZARpillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
            var ZARcurveOIS0 = new IrCurve(ZARpillarDatesOIS, new double[ZARpillarDatesOIS.Length], startDate, "ZAR.DISC.CSA_ZAR", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
            var USDcurve3m0 = new IrCurve(USDpillarDates3m, new double[USDpillarDates3m.Length], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };
            var USDcurveOIS0 = new IrCurve(USDpillarDatesOIS, new double[USDpillarDatesOIS.Length], startDate, "USD.DISC.CSA_USD", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };

            var engine0 = new FundingModel(startDate, new IrCurve[] { ZARcurve3m0, ZARcurveOIS0, USDcurve3m0, USDcurveOIS0 }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);


            var S = new NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian()
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001,
                MaxItterations = IsCoverageOnly ? 1 : 100,
            };
            var S0 = new NewtonRaphsonMultiCurveSolverStaged()
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001,
                MaxItterations = IsCoverageOnly ? 1 : 100,
            };

            S.Solve(engine, FIC);
            S0.Solve(engine0, FIC);

            if (!IsCoverageOnly)
            {
                foreach (var ins in FIC)
                {
                    var pv = ins.Pv(engine, false);
                    Assert.Equal(0.0, pv, 7);
                }

                foreach (var curve in engine.Curves)
                {
                    var otherCurve = engine0.Curves[curve.Key] as IrCurve;
                    Assert.Equal(curve.Value.NumberOfPillars, otherCurve.NumberOfPillars);
                    var otherRates = otherCurve.GetRates();
                    var rates = (curve.Value as IrCurve).GetRates();
                    for (var i = 0; i < otherRates.Length; i++)
                    {
                        Assert.Equal(otherRates[i], rates[i], 10);
                    }
                }
            }
        }

        [Fact]
        public void ComplexerCurve()
        {
            var startDate = new DateTime(2016, 05, 20);
            Frequency[] depoTenors = { 3.Months() };
            Frequency[] OISdepoTenors = { 1.Bd() };
            double[] depoPricesZAR = { 0.06 };
            double[] depoPricesUSD = { 0.01 };
            double[] OISdepoPricesZAR = { 0.055 };
            double[] OISdepoPricesUSD = { 0.009 };

            string[] FRATenors = { "3x6", "6x9", "9x12", "12x15", "15x18", "18x21", "21x24" };
            double[] FRAPricesZAR = { 0.065, 0.07, 0.075, 0.077, 0.08, 0.081, 0.082 };
            double[] FRAPricesUSD = { 0.012, 0.013, 0.014, 0.015, 0.016, 0.017, 0.018 };

            Frequency[] swapTenors = { 3.Years(), 4.Years(), 5.Years(), 6.Years(), 7.Years(), 8.Years(), 9.Years(), 10.Years(), 12.Years(), 15.Years(), 20.Years(), 25.Years(), 30.Years() };
            double[] swapPricesZAR = { 0.08, 0.083, 0.085, 0.087, 0.089, 0.091, 0.092, 0.093, 0.094, 0.097, 0.099, 0.099, 0.099 };
            double[] swapPricesUSD = { 0.017, 0.018, 0.019, 0.020, 0.021, 0.022, 0.023, 0.024, 0.025, 0.026, 0.027, 0.028, 0.03 };
            Frequency[] oisTenors = { 3.Months(), 6.Months(), 1.Years(), 18.Months(), 2.Years(), 3.Years(), 4.Years(), 5.Years(), 6.Years(), 7.Years(), 8.Years(), 9.Years(), 10.Years(), 12.Years(), 15.Years(), 20.Years(), 25.Years(), 30.Years() };
            double[] oisPricesZAR = { 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004, 0.004 };
            double[] oisPricesUSD = { 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002, 0.002 };

            var fxSpot = 14.0;
            Frequency[] fxForwardTenors = { 3.Months(), 6.Months(), 1.Years(), 18.Months(), 2.Years(), 3.Years() };
            double[] fxForwardPrices = { 14.10, 14.20, 14.40, 14.60, 14.80, 15.20 };
            Frequency[] xcySwapTenors = { 4.Years(), 5.Years(), 6.Years(), 7.Years(), 8.Years(), 9.Years(), 10.Years(), 12.Years(), 15.Years(), 20.Years(), 25.Years(), 30.Years() };
            double[] xcySwapPrices = { 0.0055, 0.0050, 0.0045, 0.0040, 0.0035, 0.0030, 0.0025, 0.0020, 0.0015, 0.0010, 0.0005, 0.0000 };

            var ZARpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var ZARpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDates3m = ZARpillarDatesDepo.Union(ZARpillarDatesSwap).Union(ZARpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            var ZARpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesOIS = ZARpillarDatesDepoOIS.Union(ZARpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();


            var USDpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var USDpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDates3m = USDpillarDatesDepo.Union(USDpillarDatesSwap).Union(USDpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            var USDpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var USDpillarDatesOIS = USDpillarDatesDepoOIS.Union(USDpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();

            var fxForwardPillarDates = fxForwardTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var xcySwapDates = xcySwapTenors.Select(x => startDate.AddPeriod(RollType.MF, _usd, x)).ToArray();
            var fxPillarDates = fxForwardPillarDates.Union(xcySwapDates).Distinct().OrderBy(x => x).ToArray();


            var ZARswaps = new IrSwap[swapTenors.Length];
            var ZARdepos = new IrSwap[depoTenors.Length];
            var ZARdeposOIS = new IrSwap[OISdepoTenors.Length];
            var ZARoisSwaps = new IrBasisSwap[oisTenors.Length];
            var ZARFRAs = new ForwardRateAgreement[FRATenors.Length];

            var USDswaps = new IrSwap[swapTenors.Length];
            var USDdepos = new IrSwap[depoTenors.Length];
            var USDdeposOIS = new IrSwap[OISdepoTenors.Length];
            var USDoisSwaps = new IrBasisSwap[oisTenors.Length];
            var USDFRAs = new ForwardRateAgreement[FRATenors.Length];

            var fxForwards = new FxForward[fxForwardTenors.Length];
            var xcySwaps = new XccyBasisSwap[xcySwapTenors.Length];

            var FIC = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            for (var i = 0; i < FRATenors.Length; i++)
            {
                ZARFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesZAR[i], _zar3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARFRAs[i]);
                USDFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesUSD[i], usd3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDFRAs[i]);
            }

            for (var i = 0; i < oisTenors.Length; i++)
            {
                ZARoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesZAR[i], true, zaron, _zar3m, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                FIC.Add(ZARoisSwaps[i]);
                USDoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesUSD[i], true, usdon, usd3m, "USD.LIBOR.3M", "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                FIC.Add(USDoisSwaps[i]);
            }

            for (var i = 0; i < swapTenors.Length; i++)
            {
                ZARswaps[i] = new IrSwap(startDate, swapTenors[i], _zar3m, swapPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARswaps[i]);
                USDswaps[i] = new IrSwap(startDate, swapTenors[i], usd3m, swapPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDswaps[i]);

            }

            for (var i = 0; i < depoTenors.Length; i++)
            {
                ZARdepos[i] = new IrSwap(startDate, depoTenors[i], _zar3m, depoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARdepos[i]);
                USDdepos[i] = new IrSwap(startDate, depoTenors[i], usd3m, depoPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                FIC.Add(USDdepos[i]);
            }

            for (var i = 0; i < OISdepoTenors.Length; i++)
            {
                ZARdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], zaron, OISdepoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                FIC.Add(ZARdeposOIS[i]);
                USDdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], usdon, OISdepoPricesUSD[i], SwapPayReceiveType.Payer, "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                FIC.Add(USDdeposOIS[i]);
            }

            for (var i = 0; i < fxForwards.Length; i++)
            {
                fxForwards[i] = new FxForward
                {
                    SolveCurve = "ZAR.DISC.CSA_USD",
                    DeliveryDate = fxForwardPillarDates[i],
                    DomesticCCY = ccyUsd,
                    ForeignCCY = ccyZar,
                    DomesticQuantity = 1e6 / fxForwardPrices[i],
                    Strike = fxForwardPrices[i],
                    ForeignDiscountCurve = "ZAR.DISC.CSA_USD",
                };
                FIC.Add(fxForwards[i]);
            }

            for (var i = 0; i < xcySwapTenors.Length; i++)
            {
                xcySwaps[i] = new XccyBasisSwap(startDate, xcySwapTenors[i], xcySwapPrices[i], true, usd3m, _zar3m, ExchangeType.Both, MTMSwapType.ReceiveNotionalFixed, "USD.LIBOR.3M", "ZAR.JIBAR.3M", "USD.DISC.CSA_USD", "ZAR.DISC.CSA_USD") { SolveCurve = "ZAR.DISC.CSA_USD" };
                FIC.Add(xcySwaps[i]);
            }

            var ZARcurve3m = new IrCurve(ZARpillarDates3m, new double[ZARpillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
            var ZARcurveOIS = new IrCurve(ZARpillarDatesOIS, new double[ZARpillarDatesOIS.Length], startDate, "ZAR.DISC.CSA_ZAR", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
            var USDcurve3m = new IrCurve(USDpillarDates3m, new double[USDpillarDates3m.Length], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };
            var USDcurveOIS = new IrCurve(USDpillarDatesOIS, new double[USDpillarDatesOIS.Length], startDate, "USD.DISC.CSA_USD", Interpolator1DType.LinearFlatExtrap, ccyUsd) { SolveStage = 1 };
            var fxCurve = new IrCurve(fxPillarDates, Enumerable.Repeat(0.05,fxPillarDates.Length).ToArray(), startDate, "ZAR.DISC.CSA_USD", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 2 };


            var engine = new FundingModel(startDate, new IrCurve[] { ZARcurve3m, ZARcurveOIS, USDcurve3m, USDcurveOIS, fxCurve }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var fxMatrix = new FxMatrix(TestProviderHelper.CurrencyProvider);
            var spotRates = new Dictionary<Currency, double>
            {
                {ccyZar,fxSpot}
            };
            var fxPairs = new List<FxPair>
            {
                new FxPair { Domestic=ccyUsd, Foreign=ccyZar, PrimaryCalendar=_usd, SpotLag =new Frequency("2b") }
            };
            var discountMap = new Dictionary<Currency, string>
            {
                {ccyUsd,"USD.DISC.CSA_USD" },
                {ccyZar,"ZAR.DISC.CSA_USD" },
            };

            fxMatrix.Init(ccyUsd, startDate, spotRates, fxPairs, discountMap);
            engine.SetupFx(fxMatrix);

            var S = new NewtonRaphsonMultiCurveSolverStaged()
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001,
                MaxItterations = IsCoverageOnly ? 1 : 100,
            };
            S.Solve(engine, FIC);

            if (!IsCoverageOnly)
            {
                foreach (var ins in FIC)
                {
                    var pv = ins.Pv(engine, false);
                    Assert.Equal(0.0, pv, 7);
                }
            }

        }

        [Fact]
        public void LessComplexCurve()
        {
            var startDate = new DateTime(2016, 05, 20);
            var depoTenors = new Frequency[] { 3.Months() };
            var OISdepoTenors = new Frequency[] { 1.Bd() };
            double[] depoPricesZAR = { 0.06 };

            string[] FRATenors = { "3x6", "6x9", "9x12", "12x15", "15x18", "18x21", "21x24" };
            double[] FRAPricesZAR = { 0.065, 0.07, 0.075, 0.077, 0.08, 0.081, 0.082 };

            var ZARpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, x)).ToArray();
            var ZARpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, JHB, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            var ZARpillarDates3m = ZARpillarDatesDepo.Union(ZARpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();



            var ZARdepos = new IrSwap[depoTenors.Length];
            var ZARFRAs = new ForwardRateAgreement[FRATenors.Length];

            var FIC = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            for (var i = 0; i < FRATenors.Length; i++)
            {
                ZARFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesZAR[i], _zar3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "ZAR.JIBAR.3M", "ZAR.JIBAR.3M") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARFRAs[i]);
            }

            for (var i = 0; i < depoTenors.Length; i++)
            {
                ZARdepos[i] = new IrSwap(startDate, depoTenors[i], _zar3m, depoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.JIBAR.3M") { SolveCurve = "ZAR.JIBAR.3M" };
                FIC.Add(ZARdepos[i]);
             }



            var ZARcurve3m = new IrCurve(ZARpillarDates3m, new double[ZARpillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap, ccyZar) { SolveStage = 0 };
        
            var engine = new FundingModel(startDate, new IrCurve[] { ZARcurve3m }, TestProviderHelper.CurrencyProvider, TestProviderHelper.CalendarProvider);

            var S = new NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian()
            {
                Tollerance = IsCoverageOnly ? 1 : 0.00000001,
                MaxItterations = IsCoverageOnly ? 1 : 100,
            };
            S.Solve(engine, FIC);

            if (!IsCoverageOnly)
            {
                foreach (var ins in FIC)
                {
                    var pv = ins.Pv(engine, false);
                    Assert.Equal(0.0, pv, 7);
                }
            }
        }
    }
}
