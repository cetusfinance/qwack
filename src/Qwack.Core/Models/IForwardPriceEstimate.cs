namespace Qwack.Core.Models
{
    public interface IForwardPriceEstimate : IPathProcess, IRequiresFinish
    {
        public double GetEstimate(double? spot);
    }
}
