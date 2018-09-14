namespace Qwack.Core.Models
{
    public interface ICorrelationMatrix
    {
        double[,] Correlations { get; set; }
        string[] LabelsX { get; set; }
        string[] LabelsY { get; set; }

        double GetCorrelation(string label1, string label2);

        ICorrelationMatrix Clone();
        ICorrelationMatrix Bump(double epsilon);
    }
}
