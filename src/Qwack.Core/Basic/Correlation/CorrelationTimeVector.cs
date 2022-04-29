using System;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Correlations;

namespace Qwack.Core.Basic.Correlation
{
    public class CorrelationTimeVector : ICorrelationMatrix
    {
        public string[] LabelsX { get; set; }
        public string[] LabelsY { get; set; }
        public double[][] Correlations { get; set; }

        private readonly IInterpolator1D[] _interps;
        private readonly double[] _times;
        private readonly Interpolator1DType _interpType;

        public CorrelationTimeVector() { }

        public CorrelationTimeVector(string labelX, string labelY, double[] correlations, double[] times, Interpolator1DType interpKind = Interpolator1DType.LinearFlatExtrap) :
            this(labelX, new[] { labelY }, new[] { correlations }, times, interpKind)
        {
        }

        public CorrelationTimeVector(string labelX, string[] labelsY, double[][] correlations, double[] times, Interpolator1DType interpKind = Interpolator1DType.LinearFlatExtrap)
            : this()
        {
            LabelsX = new[] { labelX };
            LabelsY = labelsY;
            Correlations = correlations;
            _times = times;
            _interpType = interpKind;
            _interps = correlations.Select(c => InterpolatorFactory.GetInterpolator(times, c, interpKind)).ToArray();
        }

        public double GetCorrelation(string label1, string label2, double t = 0)
        {
            var ix1 = Array.IndexOf(LabelsY, label1);
            ix1 = ix1 < 0 ? 0 : ix1;
            var ix2 = Array.IndexOf(LabelsY, label2);
            ix2 = ix2 < 0 ? 0 : ix2;

            if (LabelsX[0] == label1 && LabelsY[ix2] == label2)
            {
                return _interps[ix2].Interpolate(t);
            }
            else if (LabelsX[0] == label2 && LabelsY[ix1] == label1)
            {
                return _interps[ix1].Interpolate(t);
            }

            throw new Exception($"Correlation not found for {label1}/{label2}");
        }

        public bool TryGetCorrelation(string label1, string label2, out double correl, double t = 0)
        {
            var ix1 = Array.IndexOf(LabelsY, label1);
            ix1 = ix1 < 0 ? 0 : ix1;
            var ix2 = Array.IndexOf(LabelsY, label2);
            ix2 = ix2 < 0 ? 0 : ix2;

            if (LabelsX[0] == label1 && LabelsY[ix2] == label2)
            {
                correl = _interps[ix2].Interpolate(t);
                return true;
            }
            else if (LabelsX[0] == label2 && LabelsY[ix1] == label1)
            {
                correl = _interps[ix1].Interpolate(t);
                return true;
            }

            correl = double.NaN;
            return false;
        }

        public ICorrelationMatrix Clone() => new CorrelationTimeVector(LabelsX[0], LabelsY, Correlations, _times, _interpType);

        public ICorrelationMatrix Bump(double epsilon)
        {
            var bumpedCorrels = new double[Correlations.Length][];

            for (var a = 0; a < bumpedCorrels.Length; a++)
            {
                bumpedCorrels[a] = new double[Correlations[a].Length];
                for (var i = 0; i < bumpedCorrels.Length; i++)
                    bumpedCorrels[a][i] = Correlations[a][i] + epsilon * (1 - Correlations[a][i]);
            }
            return new CorrelationTimeVector(LabelsX[0], LabelsY, bumpedCorrels, _times, _interpType);
        }

        public TO_CorrelationMatrix GetTransportObject() =>
            new()
            {
                Correlations = Correlations,
                LabelsX = LabelsX,
                LabelsY = LabelsY,
                InterpolatorType = _interpType,
                IsTimeVector = true,
                Times = _times
            };
    }
}
