using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public interface IPriceCurve 
    {
        DateTime BuildDate { get; }
        
        double GetPriceForDate(DateTime date);
        double GetPriceForFixingDate(DateTime date);

        double GetAveragePriceForDates(DateTime[] dates);

        string Name { get; set; }

        string AssetId { get; }

        Currency Currency { get; set; }

        int NumberOfPillars { get; }

        Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump);

        IPriceCurve RebaseDate(DateTime newAnchorDate);

        bool UnderlyingsAreForwards { get; }

        DateTime[] PillarDates { get; }

        DateTime PillarDatesForLabel(string label);

        PriceCurveType CurveType { get; }

        Frequency SpotLag { get; set; } 
        Calendar SpotCalendar { get; set; }
    }
}
