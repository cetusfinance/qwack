using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;

namespace Qwack.Core.Descriptors
{
    public class ForecastCurveDescriptor : MarketDataDescriptor
    {
        public FloatRateIndex Index { get; set; }
    }
}
