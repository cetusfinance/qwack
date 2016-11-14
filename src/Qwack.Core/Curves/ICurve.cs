using System;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public interface ICurve
    {
        DateTime BuildDate { get; }
        double GetDf(DateTime startDate, DateTime endDate);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis);
        string Name { get; }
        ICurve SetRate(int pillarIx, double rate, bool mutate);
        int NumberOfPillars { get; }
        double GetRate(int pillarIx);
        IrCurve BumpRate(int pillarIx, double delta, bool mutate);
    }
}
