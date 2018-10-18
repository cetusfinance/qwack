using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Descriptors;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public interface IPriceCurve : IHasDescriptors
    {
        DateTime BuildDate { get; }
        
        double GetPriceForDate(DateTime date);
    
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
    }
}
