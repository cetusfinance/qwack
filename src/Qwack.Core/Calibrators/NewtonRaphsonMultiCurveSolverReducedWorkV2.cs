using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonMultiCurveSolverReducedWorkV2
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double _jacobianBump = 0.0001;

        private double[][] _jacobian;
        double[] _currentPvs;

        public void Solve(FundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            var maxStage = fundingModel.Curves.Max(x => x.Value.SolveStage);
            var curvesForStage = new List<ICurve>();
            var fundingInstruments = new List<IFundingInstrument>();
            for (int stage = 0; stage <= maxStage; stage++)
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
                ComputeJacobian(fundingInstruments, fundingModel, curvesForStage, currentGuess, bumpedPvs);
                ComputeNextGuess(currentGuess, fundingInstruments.Count, curvesForStage);
                for (int i = 0; i < MaxItterations; i++)
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

        void ComputeNextGuess(double[] currentGuess, int numberOfInstruments, List<ICurve> curvesForStage)
        {
            // f = f - d/f'
            var JacobianMI = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPvs, JacobianMI);
            int curveIx = 0;
            int pillarIx = 0;
            for (int j = 0; j < numberOfInstruments; j++)
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

        private void ComputeJacobian(List<IFundingInstrument> instruments, FundingModel model, List<ICurve> curvesForStage, double[] currentGuess, double[] bumpedPvs)
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(instruments.Count, instruments.Count);
            int curveIx = 0;
            int pillarIx = 0;
            for (int i = 0; i < instruments.Count; i++)
            {
                var currentCurve = curvesForStage[curveIx];
                model.CurrentSolveCurve = currentCurve.Name;
                currentGuess[i] = currentCurve.GetRate(pillarIx);
                currentCurve.BumpRate(pillarIx, _jacobianBump, true);
                ComputePVs(false, instruments, model, bumpedPvs);
                currentCurve.BumpRate(pillarIx, -_jacobianBump, true);

                for (int j = 0; j < bumpedPvs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPvs[j] - _currentPvs[j]) / _jacobianBump;
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
        private void ComputePVs(bool updateState, List<IFundingInstrument> instruments, FundingModel model, double[] currentPvs)
        {
            for (int i = 0; i < currentPvs.Length; i++)
            {
                currentPvs[i] = instruments[i].Pv(model, updateState);
            }
        }
    }
}
