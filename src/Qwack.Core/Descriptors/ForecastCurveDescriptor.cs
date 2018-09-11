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

        public override bool Equals(object obj) => obj is ForecastCurveDescriptor descriptor &&
                   EqualityComparer<FloatRateIndex>.Default.Equals(Index, descriptor.Index);

        public override int GetHashCode()
        {
            return -2134847229 + EqualityComparer<FloatRateIndex>.Default.GetHashCode(Index);
        }
    }
}
