using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Descriptors
{
    public class AssetCurveDescriptor : MarketDataDescriptor
    {
        public string AssetId { get; set; }
        public Currency Currency { get; set; }

        public override bool Equals(object obj) => obj is AssetCurveDescriptor descriptor &&
                   AssetId == descriptor.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(Currency, descriptor.Currency);

        public override int GetHashCode()
        {
            var hashCode = 159653148;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetId);
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(Currency);
            return hashCode;
        }
    }
}
