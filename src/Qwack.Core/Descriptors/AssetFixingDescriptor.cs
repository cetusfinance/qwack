using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Descriptors
{
    public class AssetFixingDescriptor : MarketDataDescriptor
    {
        public string AssetId { get; set; }
        public DateTime FixingDate { get; set; }

        public override bool Equals(object obj) => obj is AssetFixingDescriptor descriptor &&
                   AssetId == descriptor.AssetId &&
                   FixingDate == descriptor.FixingDate;

        public override int GetHashCode()
        {
            var hashCode = 2077444234;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetId);
            hashCode = hashCode * -1521134295 + FixingDate.GetHashCode();
            return hashCode;
        }
    }
}
