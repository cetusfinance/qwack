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
    }
}
