using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonMultiCurveSolverStagedWithAnalyticJacobian
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private double[][] _jacobian;
        double[] _currentPvs;

        public void Solve(FundingModel fundingModel, FundingInstrumentCollection instruments)
        {
            var maxStage = fundingModel.Curves.Max(x => x.Value.SolveStage);
            var curvesForStage = new List<ICurve>();
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
                    ComputeJacobian(fundingInstruments, fundingModel, curvesForStage);
                    ComputeNextGuess(currentGuess, fundingInstruments.Count, curvesForStage);
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeNextGuess(double[] currentGuess, int numberOfInstruments, List<ICurve> curvesForStage)
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

        private void ComputeJacobian(List<IFundingInstrument> instruments, FundingModel model, List<ICurve> curvesForStage)
        {
            var nPillars = curvesForStage.Sum(x => x.NumberOfPillars);
            
            _jacobian = new double[instruments.Count][];
            for(var i=0;i<_jacobian.Length;i++)
                _jacobian[i] = new double[nPillars];

            for (var i = 0; i < instruments.Count; i++)
            {
                var sensitivities = instruments[i].Sensitivities(model);
                var pillarOffset = 0;
                foreach (var curve in curvesForStage)
                {
                    if (sensitivities.ContainsKey(curve.Name))
                        foreach (var date in sensitivities[curve.Name].Keys)
                        {
                            var s1 = curve.GetSensitivity(date);
                            for (var p = 0; p < s1.Length; p++)
                                _jacobian[pillarOffset + p][i] += s1[p]* sensitivities[curve.Name][date];
                        }

                    pillarOffset += curve.NumberOfPillars;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputePVs(bool updateState, List<IFundingInstrument> instruments, FundingModel model, double[] currentPvs)
        {
            for (var i = 0; i < currentPvs.Length; i++)
            {
                currentPvs[i] = instruments[i].Pv(model, updateState);
            }
        }
    }
}
