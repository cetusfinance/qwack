using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Calibrators
{
    public class NewtonRaphsonMultiCurveSolverStaged
    {
        public double Tollerance { get; set; } = 0.0000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }

        public bool InLineCurveGuessing { get; set; }

        private const double JacobianBump = 0.00001;

        //private double[][] _jacobian;
        //double[] _currentPvs;

        public void Solve(IFundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            var sw = new Stopwatch();
            sw.Start();
            var itterationsPerStage = new Dictionary<int, int>();
            var curvesPerStange = new Dictionary<int, string>();

            var maxStage = fundingModel.Curves.Max(x => (x.Value as IrCurve).SolveStage);
            var curvesForStage = new List<IIrCurve>();
            var fundingInstruments = new List<IFundingInstrument>();
            for (var stage = 0; stage <= maxStage; stage++)
            {
                curvesForStage.Clear();
                fundingInstruments.Clear();

                foreach (var kv in fundingModel.Curves)
                {
                    if ((kv.Value as IrCurve).SolveStage == stage)
                    {
                        var insForCurve = new List<IFundingInstrument>();
                        curvesForStage.Add(kv.Value);
                        foreach (var inst in instruments)
                        {
                            if (inst.SolveCurve == kv.Value.Name)
                            {
                                insForCurve.Add(inst);
                                fundingInstruments.Add(inst);
                            }
                        }

                        if (InLineCurveGuessing)
                        {
                            var points = insForCurve.ToDictionary(x => x.PillarDate, x => x.SuggestPillarValue(fundingModel));
                            for (var i = 0; i < kv.Value.NumberOfPillars; i++)
                            {
                                kv.Value.SetRate(i, points[(kv.Value as IrCurve).PillarDates[i]], true);
                            }
                        }
                    }
                }
                curvesPerStange[stage] = string.Join(",", curvesForStage.Select(c => c.Name).ToArray());
                var currentGuess = new double[fundingInstruments.Count];
                var currentPvs = new double[fundingInstruments.Count];
                var bumpedPvs = new double[fundingInstruments.Count];
                var jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(fundingInstruments.Count, fundingInstruments.Count);

                for (var i = 0; i < MaxItterations; i++)
                {
                    ComputePVs(true, fundingInstruments, fundingModel, currentPvs);
                    if (currentPvs.Max(x => System.Math.Abs(x)) < Tollerance)
                    {
                        UsedItterations += i + 1;
                        itterationsPerStage[stage] = i + 1;
                        break;
                    }
                    ComputeJacobian(fundingInstruments, fundingModel, curvesForStage, currentGuess, bumpedPvs, currentPvs, ref jacobian);
                    ComputeNextGuess(currentGuess, fundingInstruments.Count, curvesForStage, jacobian, currentPvs);
                }
            }

            fundingModel.CalibrationItterations = itterationsPerStage;
            fundingModel.CalibrationTimeMs = sw.ElapsedMilliseconds;
            fundingModel.CalibrationCurves = curvesPerStange;
            sw.Stop();
        }

        public void Solve(IFundingModel fundingModel, FundingInstrumentCollection instruments, Dictionary<string, SolveStage> stages)
        {
            var sw = new Stopwatch();
            sw.Start();
            var itterationsPerStage = new Dictionary<SolveStage, int>();

            var maxStage = stages.Values.Max(x => x.Stage);

            for (var stage = 0; stage <= maxStage; stage++)
            {
                var inThisStage = stages.Where(x => x.Value.Stage == stage)
                    .GroupBy(x => x.Value.SubStage)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.Key).ToArray());

                ParallelUtils.Instance.Foreach(inThisStage.Keys.ToList(), subStage =>
                {

                    var curvesForStage = inThisStage[subStage].Select(c => (IIrCurve)fundingModel.GetCurve(c)).ToList();
                    var fundingInstruments = new List<IFundingInstrument>();
                    var insForCurve = new List<IFundingInstrument>();
                    foreach (var curve in curvesForStage)
                    {
                        foreach (var inst in instruments)
                        {
                            if (inst.SolveCurve == curve.Name)
                            {
                                insForCurve.Add(inst);
                                fundingInstruments.Add(inst);
                            }
                        }

                        if (InLineCurveGuessing)
                        {
                            var points = insForCurve.ToDictionary(x => x.PillarDate, x => x.SuggestPillarValue(fundingModel));
                            for (var i = 0; i < curve.NumberOfPillars; i++)
                            {
                                curve.SetRate(i, points[((IrCurve)curve).PillarDates[i]], true);
                            }
                        }

                    }
                    var currentGuess = new double[fundingInstruments.Count];
                    var currentPvs = new double[fundingInstruments.Count];
                    var bumpedPvs = new double[fundingInstruments.Count];
                    var jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(fundingInstruments.Count, fundingInstruments.Count);

                    for (var i = 0; i < MaxItterations; i++)
                    {
                        ComputePVs(true, fundingInstruments, fundingModel, currentPvs);
                        if (currentPvs.Max(x => System.Math.Abs(x)) < Tollerance)
                        {
                            UsedItterations += i + 1;
                            break;
                        }
                        ComputeJacobian(fundingInstruments, fundingModel, curvesForStage, currentGuess, bumpedPvs, currentPvs, ref jacobian);
                        ComputeNextGuess(currentGuess, fundingInstruments.Count, curvesForStage, jacobian, currentPvs);
                    }
                }).Wait();
            }
            //fundingModel.CalibrationItterations = itterationsPerStage;
            fundingModel.CalibrationTimeMs = sw.ElapsedMilliseconds;
            sw.Stop();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeNextGuess(double[] currentGuess, int numberOfInstruments, List<IIrCurve> curvesForStage, double[][] jacobian, double[] currentPvs)
        {
            // f = f - d/f'
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(currentPvs, jacobianMi);
            var curveIx = 0;
            var pillarIx = 0;
            for (var j = 0; j < numberOfInstruments; j++)
            {
                var curve = curvesForStage[curveIx];
                currentGuess[j] -= deltaGuess[j];

                curve.SetRate(pillarIx, currentGuess[j], true);
                pillarIx++;
                if (pillarIx == curve.NumberOfPillars)
                {
                    pillarIx = 0;
                    curveIx++;
                }
            }
        }

        private void ComputeJacobian(List<IFundingInstrument> instruments, IFundingModel model, List<IIrCurve> curvesForStage, double[] currentGuess, double[] bumpedPvs, double[] currentPvs, ref double[][] jacobian)
        {
            var curveIx = 0;
            var pillarIx = 0;
            for (var i = 0; i < instruments.Count; i++)
            {
                var currentCurve = curvesForStage[curveIx];
                model.CurrentSolveCurve = currentCurve.Name;
                currentGuess[i] = currentCurve.GetRate(pillarIx);
                currentCurve.BumpRate(pillarIx, JacobianBump, true);
                ComputePVs(false, instruments, model, bumpedPvs);
                currentCurve.BumpRate(pillarIx, -JacobianBump, true);

                for (var j = 0; j < bumpedPvs.Length; j++)
                {
                    jacobian[i][j] = (bumpedPvs[j] - currentPvs[j]) / JacobianBump;
                }
                pillarIx++;
                if (pillarIx == currentCurve.NumberOfPillars)
                {
                    pillarIx = 0;
                    curveIx++;
                }
            }
            model.CurrentSolveCurve = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputePVs(bool updateState, List<IFundingInstrument> instruments, IFundingModel model, double[] currentPvs)
        {
            for (var i = 0; i < currentPvs.Length; i++)
            {
                currentPvs[i] = instruments[i].Pv(model, updateState);
            }
        }
    }
}
