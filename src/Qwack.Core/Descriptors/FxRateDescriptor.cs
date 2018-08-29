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
    }
}
