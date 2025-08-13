using System;
using System.Collections.Generic;
using Qwack.Dates;

namespace Qwack.Core.Models
{
    public class ForwardPriceEstimatorSpec
    {
        public DateTime ValDate { get; set; }
        public DateTime[] AverageDates { get; set; }
        public string AssetId { get; set; }
        public DateShifter DateShifter { get; set; } 
        public IForwardPriceEstimate Estimator { get; set; }

        public override bool Equals(object obj) => obj is ForwardPriceEstimatorSpec spec && 
            ValDate == spec.ValDate && 
            EqualityComparer<DateTime[]>.Default.Equals(AverageDates, spec.AverageDates) && 
            AssetId == spec.AssetId;
        public override int GetHashCode() => HashCode.Combine(ValDate, AverageDates, AssetId);
    }
}
