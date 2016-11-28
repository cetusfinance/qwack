using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;

namespace Qwack.Curves.Benchmark
{
    [Config(typeof(SolveConfig))]
    public class SolvingOisBenchmark
    {
        private static FundingModel _fundingModel;
        private static FundingInstrumentCollection _instruments;
        
        [Setup]
        public static void Setup()
        {
            DateTime startDate = new DateTime(2016, 05, 20);
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

            DateTime[] ZARpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._jhb, x)).ToArray();
            DateTime[] ZARpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._jhb, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            DateTime[] ZARpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._jhb, x)).ToArray();
            DateTime[] ZARpillarDates3m = ZARpillarDatesDepo.Union(ZARpillarDatesSwap).Union(ZARpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            DateTime[] ZARpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._jhb, x)).ToArray();
            DateTime[] ZARpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._jhb, x)).ToArray();
            DateTime[] ZARpillarDatesOIS = ZARpillarDatesDepoOIS.Union(ZARpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();


            DateTime[] USDpillarDatesDepo = depoTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._usd, x)).ToArray();
            DateTime[] USDpillarDatesFRA = FRATenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._usd, new Frequency(x.Split('x')[1] + "M"))).ToArray();
            DateTime[] USDpillarDatesSwap = swapTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._usd, x)).ToArray();
            DateTime[] USDpillarDates3m = USDpillarDatesDepo.Union(USDpillarDatesSwap).Union(USDpillarDatesFRA).Distinct().OrderBy(x => x).ToArray();
            DateTime[] USDpillarDatesDepoOIS = OISdepoTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._usd, x)).ToArray();
            DateTime[] USDpillarDatesOISSwap = oisTenors.Select(x => startDate.AddPeriod(RollType.MF, CurveDataSetup._usd, x)).ToArray();
            DateTime[] USDpillarDatesOIS = USDpillarDatesDepoOIS.Union(USDpillarDatesOISSwap).Distinct().OrderBy(x => x).ToArray();


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


            _instruments = new FundingInstrumentCollection();
            
            for (int i = 0; i < FRATenors.Length; i++)
            {
                ZARFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesZAR[i], CurveDataSetup._zar3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                _instruments.Add(ZARFRAs[i]);
                USDFRAs[i] = new ForwardRateAgreement(startDate, FRATenors[i], FRAPricesUSD[i], CurveDataSetup.usd3m, SwapPayReceiveType.Payer, FraDiscountingType.Isda, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                _instruments.Add(USDFRAs[i]);
            }

            for (int i = 0; i < oisTenors.Length; i++)
            {
                ZARoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesZAR[i], true, CurveDataSetup.zaron, CurveDataSetup._zar3m, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                _instruments.Add(ZARoisSwaps[i]);
                USDoisSwaps[i] = new IrBasisSwap(startDate, oisTenors[i], oisPricesUSD[i], true, CurveDataSetup.usdon, CurveDataSetup.usd3m, "USD.LIBOR.3M", "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                _instruments.Add(USDoisSwaps[i]);
            }

            for (int i = 0; i < swapTenors.Length; i++)
            {
                ZARswaps[i] = new IrSwap(startDate, swapTenors[i], CurveDataSetup._zar3m, swapPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                _instruments.Add(ZARswaps[i]);
                USDswaps[i] = new IrSwap(startDate, swapTenors[i], CurveDataSetup.usd3m, swapPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                _instruments.Add(USDswaps[i]);
            }

            for (int i = 0; i < depoTenors.Length; i++)
            {
                ZARdepos[i] = new IrSwap(startDate, depoTenors[i], CurveDataSetup._zar3m, depoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.JIBAR.3M" };
                _instruments.Add(ZARdepos[i]);
                USDdepos[i] = new IrSwap(startDate, depoTenors[i], CurveDataSetup.usd3m, depoPricesUSD[i], SwapPayReceiveType.Payer, "USD.LIBOR.3M", "USD.DISC.CSA_USD") { SolveCurve = "USD.LIBOR.3M" };
                _instruments.Add(USDdepos[i]);
            }

            for (int i = 0; i < OISdepoTenors.Length; i++)
            {
                ZARdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], CurveDataSetup.zaron, OISdepoPricesZAR[i], SwapPayReceiveType.Payer, "ZAR.DISC.CSA_ZAR", "ZAR.DISC.CSA_ZAR") { SolveCurve = "ZAR.DISC.CSA_ZAR" };
                _instruments.Add(ZARdeposOIS[i]);
                USDdeposOIS[i] = new IrSwap(startDate, OISdepoTenors[i], CurveDataSetup.usdon, OISdepoPricesUSD[i], SwapPayReceiveType.Payer, "USD.DISC.CSA_USD", "USD.DISC.CSA_USD") { SolveCurve = "USD.DISC.CSA_USD" };
                _instruments.Add(USDdeposOIS[i]);
            }

            var ZARcurve3m = new IrCurve(ZARpillarDates3m, new double[ZARpillarDates3m.Length], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap) { SolveStage = 0 };
            var ZARcurveOIS = new IrCurve(ZARpillarDatesOIS, new double[ZARpillarDatesOIS.Length], startDate, "ZAR.DISC.CSA_ZAR", Interpolator1DType.LinearFlatExtrap) { SolveStage = 0 };
            var USDcurve3m = new IrCurve(USDpillarDates3m, new double[USDpillarDates3m.Length], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap) { SolveStage = 1 };
            var USDcurveOIS = new IrCurve(USDpillarDatesOIS, new double[USDpillarDatesOIS.Length], startDate, "USD.DISC.CSA_USD", Interpolator1DType.LinearFlatExtrap) { SolveStage = 1 };

            _fundingModel = new FundingModel(startDate, new IrCurve[] { ZARcurve3m, ZARcurveOIS, USDcurve3m, USDcurveOIS });
        }

        [Benchmark(Baseline =true)]
        public static void InitialOisAttempt()
        {
            var model = _fundingModel.DeepClone();
            var solver = new Core.Calibrators.NewtonRaphsonMultiCurveSolver();
            solver.Solve(model,_instruments);
        }
                
        [Benchmark]
        public static void PuttingItTogether()
        {
            var model = _fundingModel.DeepClone();
            var solver = new Core.Calibrators.NewtonRaphsonMultiCurveSolverStaged();
            solver.Solve(model, _instruments);
        }
    }
}
