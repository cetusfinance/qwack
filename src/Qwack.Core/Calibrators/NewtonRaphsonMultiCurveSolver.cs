using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonMultiCurveSolver
    {
        private const double _jacobianBump = 0.0001;
        public int nInstruments { get; private set; }
        public int nPillars { get; private set; }
        public FundingInstrumentCollection Instruments { get; private set; }
        public FundingModel CurveEngine { get; private set; }

        public double Tollerance { get; set; } = 0.0000001;
        public int MaxItterations { get; set; } = 1000;

        public TimeSpan SolveTime { get; set; }
        public int UsedItterations { get; set; }
        public double[][] Jacobian { get; set; }
        public double[] CurrentGuess { get; set; }
        public double[] CurrentPVs { get; set; }

        string[] curveNames;
        public void Solve(FundingModel curveEngine, FundingInstrumentCollection instruments)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            CurveEngine = curveEngine;
            Instruments = instruments;
            nInstruments = instruments.Count;
            nPillars = 0;
            curveNames = CurveEngine.Curves.Keys.ToArray();
            for (int i = 0; i < curveEngine.Curves.Count; i++)
                nPillars += curveEngine.Curves[curveNames[i]].NumberOfPillars;
            CurrentGuess = new double[nInstruments];
            CurrentPVs = ComputePVs();

            ComputeJacobian();

            for (int i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                CurrentPVs = ComputePVs();
                if (CurrentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }

            SolveTime = sw.Elapsed;
        }

        private double[] ComputePVs()
        {
            var O = new double[nInstruments];
            for (int i = 0; i < O.Length; i++)
            {
                O[i] = Instruments[i].Pv(CurveEngine, false);
            }

            return O;
        }

        private void ComputeJacobian()
        {
            Jacobian = new double[nPillars][];
            for (int i = 0; i < nPillars; i++)
            {
                Jacobian[i] = new double[nInstruments];
            }

            int currentCurveIx = 0;
            int currentCurvePillars = CurveEngine.Curves[curveNames[currentCurveIx]].NumberOfPillars;
            int pillarsSoFar = 0;
            for (int i = 0; i < nPillars; i++)
            {
                var currentEngine = CurveEngine.Curves[curveNames[currentCurveIx]];
                int pillarIx = i - pillarsSoFar;
                CurrentGuess[i] = currentEngine.GetRate(pillarIx);

                currentEngine.BumpRate(pillarIx, _jacobianBump, true);
                double[] bumpedPVs = ComputePVs();
                currentEngine.BumpRate(pillarIx, -_jacobianBump, true);

                for (int j = 0; j < bumpedPVs.Length; j++)
                {
                    Jacobian[i][j] = (bumpedPVs[j] - CurrentPVs[j]) / _jacobianBump;
                }

                if (pillarIx == currentEngine.NumberOfPillars - 1)
                {
                    pillarsSoFar += currentEngine.NumberOfPillars;
                    currentCurveIx++;
                }
            }
        }

        private void ComputeNextGuess()
        {
            // f = f - d/f'
            int currentCurveIx = 0;
            int currentCurvePillars = CurveEngine.Curves[curveNames[currentCurveIx]].NumberOfPillars;
            int pillarsSoFar = 0;
            //for (int i = 0; i < nPillars; i++)
            //{
            var currentEngine = CurveEngine.Curves[curveNames[currentCurveIx]];
            //    int pillarIx = i - pillarsSoFar;
            //var JacobianM = MathNet.Numerics.LinearAlgebra.Double.Matrix.Build.DenseOfArray(Jacobian);
            var JacobianMI = Math.Matrix.DoubleArrayFunctions.InvertMatrix(Jacobian);
            //var CurrentPVV = MathNet.Numerics.LinearAlgebra.Double.Vector.Build.Dense(CurrentPVs);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(JacobianMI, CurrentPVs);
            for (int j = 0; j < nInstruments; j++)
            {
                int pillarIx = j - pillarsSoFar;
                if (pillarIx == currentEngine.NumberOfPillars)
                {
                    pillarsSoFar += currentEngine.NumberOfPillars;
                    currentCurveIx++;
                    currentEngine = CurveEngine.Curves[curveNames[currentCurveIx]];
                    pillarIx = j - pillarsSoFar;
                }

                CurrentGuess[j] -= deltaGuess[j];

                currentEngine.SetRate(pillarIx, CurrentGuess[j], true);
            }
        }
    }
}
