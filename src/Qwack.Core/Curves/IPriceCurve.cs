using System;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public interface IPriceCurve
    {
        DateTime BuildDate { get; }
        
        double GetPriceForDate(DateTime date);
    
        double GetAveragePriceForDates(DateTime[] dates);

        string Name { get; }
        
        int NumberOfPillars { get; }

    }
}
