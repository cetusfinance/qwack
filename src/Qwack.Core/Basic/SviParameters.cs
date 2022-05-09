namespace Qwack.Core.Basic
{
    public class SviRawParameters
    {
        public double A { get; set; }
        public double B { get; set; }
        public double Rho { get; set; }
        public double M { get; set; }
        public double Sigma { get; set; }
    }

    public class SviNaturalParameters
    {
        public double Delta { get; set; }
        public double Omega { get; set; }
        public double Mu { get; set; }
        public double Rho { get; set; }
        public double Eta { get; set; }
    }

    public enum SviType
    {
        Raw,
        Natural
    }
}
