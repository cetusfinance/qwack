using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Descriptors
{
    public abstract class FxRateDescriptor:MarketDataDescriptor
    {
        public Currency BaseCurrnecy { get; set; }
        public Currency ForeignCurrnecy { get; set; }

        public override bool Equals(object obj) => obj is FxRateDescriptor descriptor &&
                   EqualityComparer<Currency>.Default.Equals(BaseCurrnecy, descriptor.BaseCurrnecy) &&
                   EqualityComparer<Currency>.Default.Equals(ForeignCurrnecy, descriptor.ForeignCurrnecy);

        public override int GetHashCode()
        {
            var hashCode = -1985666665;
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(BaseCurrnecy);
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(ForeignCurrnecy);
            return hashCode;
        }
    }
}
