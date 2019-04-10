using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Interpolation;

namespace Qwack.Core.Basic
{
    public class CorrelationTimeVector : ICorrelationMatrix
    {
        public string[] LabelsX { get; set; }
        public string[] LabelsY { get; set; }
        public double[,] Correlations { get; set; }

        private IInterpolator1D _interp;
        private double[] _correls;
        private double[] _times;
        private Interpolator1DType _interpType;

        public CorrelationTimeVector() { }

        public CorrelationTimeVector(string labelX, string labelY, double[] correlations, double[] times, Interpolator1DType interpKind = Interpolator1DType.LinearFlatExtrap)
        {
            LabelsX = new[] { labelX };
            LabelsY = new[] { labelY };
            Correlations = new double [1,1];
            _correls = correlations;
            _times = times;
            _interpType = interpKind;
            _interp = InterpolatorFactory.GetInterpolator(times, correlations, interpKind);
        }

        public double GetCorrelation(string label1, string label2, double t=0)
        {
            if ((LabelsX[0]==label1 && LabelsY[0]==label2) ||
                (LabelsX[0] == label2 && LabelsY[0] == label1))
            {
                return _interp.Interpolate(t);
            }
            
            throw new Exception($"Correlation not found for {label1}/{label2}");
        }

        public bool TryGetCorrelation(string label1, string label2, out double correl, double t = 0)
        {
            if ((LabelsX[0] == label1 && LabelsY[0] == label2) ||
                (LabelsX[0] == label2 && LabelsY[0] == label1))
            {
                correl = _interp.Interpolate(t);
                return true;
            }

            correl = double.NaN;
            return false;
        }

        public ICorrelationMatrix Clone() => new CorrelationTimeVector(LabelsX[0], LabelsY[0], _correls, _times, _interpType);

        public ICorrelationMatrix Bump(double epsilon)
        {
            var bumpedCorrels = new double[_correls.Length];

            for (var i = 0; i < bumpedCorrels.Length; i++)
                    bumpedCorrels[i] = _correls[i] + epsilon * (1 - _correls[i]);
        
            return new CorrelationTimeVector(LabelsX[0], LabelsY[0], bumpedCorrels, _times, _interpType);
        }
    }
}
