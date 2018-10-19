using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Models.Models
{
    public class CorrelationMatrix : ICorrelationMatrix
    {
        public string[] LabelsX { get; set; }
        public string[] LabelsY { get; set; }
        public double[,] Correlations { get; set; }

        public CorrelationMatrix() { }

        public CorrelationMatrix(string[] labelsX, string[] labelsY, double[,] correlations)
        {
            LabelsX = labelsX;
            LabelsY = labelsY;
            Correlations = correlations;

            if (Correlations.GetLength(0) != LabelsX.Length || Correlations.GetLength(1) != LabelsY.Length)
                throw new Exception("Inconsistent dimensions between labels and data");
        }

        public double GetCorrelation(string label1, string label2)
        {
            if (LabelsX.Contains(label1) && LabelsY.Contains(label2))
            {
                var ix1 = Array.IndexOf(LabelsX, label1);
                var ix2 = Array.IndexOf(LabelsY, label2);
                return Correlations[ix1, ix2];
            }
            else if (LabelsY.Contains(label1) && LabelsX.Contains(label2))
            {
                var ix1 = Array.IndexOf(LabelsX, label2);
                var ix2 = Array.IndexOf(LabelsY, label1);
                return Correlations[ix1, ix2];
            }

            throw new Exception($"Correlation not found for {label1}/{label2}");
        }

        public bool TryGetCorrelation(string label1, string label2, out double correl)
        {
            if (LabelsX.Contains(label1) && LabelsY.Contains(label2))
            {
                var ix1 = Array.IndexOf(LabelsX, label1);
                var ix2 = Array.IndexOf(LabelsY, label2);
                correl = Correlations[ix1, ix2];
                return true;
            }
            else if (LabelsY.Contains(label1) && LabelsX.Contains(label2))
            {
                var ix1 = Array.IndexOf(LabelsX, label2);
                var ix2 = Array.IndexOf(LabelsY, label1);
                correl = Correlations[ix1, ix2];
                return true;
            }
            correl = double.NaN;
            return false;
        }

        public ICorrelationMatrix Clone() => new CorrelationMatrix((string[])LabelsX.Clone(), (string[])LabelsY.Clone(), (double[,])Correlations.Clone());

        public ICorrelationMatrix Bump(double epsilon)
        {
            var bumpedCorrels = new double[Correlations.GetLength(0), Correlations.GetLength(1)];

            for (var i = 0; i < bumpedCorrels.GetLength(0); i++)
                for (var j = 0; j < bumpedCorrels.GetLength(1); j++)
                    bumpedCorrels[i, j] = Correlations[i, j] + epsilon * (1 - Correlations[i, j]);

            return new CorrelationMatrix((string[])LabelsX.Clone(), (string[])LabelsY.Clone(), bumpedCorrels);
        }
    }
}
