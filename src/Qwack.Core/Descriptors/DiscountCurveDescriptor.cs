using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Descriptors
{
    public class DiscountCurveDescriptor : MarketDataDescriptor
    {
        public Currency Currency { get; set; }
        public string CollateralSpec { get; set; }

        public override bool Equals(object obj) => obj is DiscountCurveDescriptor descriptor &&
                   EqualityComparer<Currency>.Default.Equals(Currency, descriptor.Currency) &&
                   CollateralSpec == descriptor.CollateralSpec;

        public override int GetHashCode()
        {
            var hashCode = 316827617;
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(Currency);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CollateralSpec);
            return hashCode;
        }
    }
}
