using System.Linq;
using Qwack.Core.Models;
using Qwack.Transport.TransportObjects.MarketData.Correlations;

namespace Qwack.Core.Basic.Correlation
{
    public class CorrelationMatrixCollection : ICorrelationMatrix
    {
        public string[] LabelsX { get; set; }
        public string[] LabelsY { get; set; }
        public double[][] Correlations { get; set; }
        public ICorrelationMatrix[] Matrices { get; }

        public CorrelationMatrixCollection() { }

        public CorrelationMatrixCollection(ICorrelationMatrix[] matrices) => Matrices = matrices;

        public double GetCorrelation(string label1, string label2, double t = 0)
        {
            foreach (var m in Matrices)
            {
                if (m.TryGetCorrelation(label1, label2, out var correl, t))
                    return correl;
            }
            return 0;
        }

        public bool TryGetCorrelation(string label1, string label2, out double correl, double t = 0)
        {
            foreach (var m in Matrices)
            {
                if (m.TryGetCorrelation(label1, label2, out var c, t))
                {
                    correl = c;
                    return true;
                }
            }

            correl = double.NaN;
            return false;
        }

        public ICorrelationMatrix Clone() => new CorrelationMatrixCollection(Matrices.Select(x => x.Clone()).ToArray());

        public ICorrelationMatrix Bump(double epsilon) => new CorrelationMatrixCollection(Matrices.Select(x => x.Bump(epsilon)).ToArray());


        public TO_CorrelationMatrix GetTransportObject() =>
           new()
           {
               Children = Matrices.Select(x => GetTransportObject()).ToArray(),
           };
    }
}
