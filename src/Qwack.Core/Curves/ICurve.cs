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
    }
}
