using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Models.Calibrators
{
    public class NewtonRaphsonMultiCurveSolverStaged
    {
        public double Tollerance { get; set; } = 0.0000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.00001;

        private double[][] _jacobian;
        double[] _currentPvs;

        public void Solve(IFundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            var maxStage = fundingModel.Curves.Max(x => x.Value.SolveStage);
            var curvesForStage = new List<IIrCurve>();
            var fundingInstruments = new List<IFundingInstrument>();
            for (var stage = 0; stage <= maxStage; stage++)
            {
                curvesForStage.Clear();
                fundingInstruments.Clear();
                foreach (var kv in fundingModel.Curves)
                {
                    if (kv.Value.SolveStage == stage)
                    {
                        curvesForStage.Add(kv.Value);
                        foreach (var inst in instruments)
                        {
                            if (inst.SolveCurve == kv.Value.Name)
                            {
                                fundingInstruments.Add(inst);
                            }
                        }
                    }
                }
                var currentGuess = new double[fundingInstruments.Count];
                _currentPvs = new double[fundingInstruments.Count];
                var bumpedPvs = new double[fundingInstruments.Count];
                _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(fundingInstruments.Count, fundingInstruments.Count);

                for (var i = 0; i < MaxItterations; i++)
                {
                    ComputePVs(true, fundingInstruments, fundingModel, _currentPvs);
                    if (_currentPvs.Max(x => System.Math.Abs(x)) < Tollerance)
                    {
                        UsedItterations = i + 1;
                        break;
                    }
                    ComputeJacobian(fundingInstruments, fundingModel, curvesForStage, currentGuess, bumpedPvs);
                    ComputeNextGuess(currentGuess, fundingInstruments.Count, curvesForStage);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeNextGuess(double[] currentGuess, int numberOfInstruments, List<IIrCurve> curvesForStage)
        {
            // f = f - d/f'
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPvs, jacobianMi);
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

        private void ComputeJacobian(List<IFundingInstrument> instruments, IFundingModel model, List<IIrCurve> curvesForStage, double[] currentGuess, double[] bumpedPvs)
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
                    _jacobian[i][j] = (bumpedPvs[j] - _currentPvs[j]) / JacobianBump;
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
