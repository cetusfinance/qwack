using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public interface IPriceCurve
    {
        DateTime BuildDate { get; }
        
        double GetPriceForDate(DateTime date);
    
        double GetAveragePriceForDates(DateTime[] dates);

        string Name { get; set; }
        
        Currency Currency { get; set; }

        int NumberOfPillars { get; }

        Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize);
    }
}
