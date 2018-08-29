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
    }

    public enum SurvivalCurveType
    {
        CDS,
        RawProbability
    }

}
