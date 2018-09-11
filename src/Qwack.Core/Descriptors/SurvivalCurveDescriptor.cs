using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;

namespace Qwack.Core.Descriptors
{
    public class SurvivalCurveDescriptor : MarketDataDescriptor
    {
        public Currency Currency { get; set; }
        public string ReferenceName { get; set; }
        public SurvivalCurveType SurvivalCurveType { get; set; }

        public override bool Equals(object obj) => obj is SurvivalCurveDescriptor descriptor &&
                   EqualityComparer<Currency>.Default.Equals(Currency, descriptor.Currency) &&
                   ReferenceName == descriptor.ReferenceName &&
                   SurvivalCurveType == descriptor.SurvivalCurveType;

        public override int GetHashCode()
        {
            var hashCode = 363465725;
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(Currency);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ReferenceName);
            hashCode = hashCode * -1521134295 + SurvivalCurveType.GetHashCode();
            return hashCode;
        }
    }

    public enum SurvivalCurveType
    {
        CDS,
        RawProbability
    }

}
