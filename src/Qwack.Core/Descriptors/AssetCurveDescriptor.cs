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
    }
}
