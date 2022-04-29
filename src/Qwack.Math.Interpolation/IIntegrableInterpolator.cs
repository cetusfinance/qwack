namespace Qwack.Math.Interpolation
{
    public interface IIntegrableInterpolator : IInterpolator1D
    {
        double DefiniteIntegral(double a, double b);
    }
}
