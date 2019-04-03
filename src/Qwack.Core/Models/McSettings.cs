using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public class McSettings
    {
        public int NumberOfPaths { get; set; }
        public int NumberOfTimesteps { get; set; }
        public RandomGeneratorType Generator { get; set; }
        public DateTime[] ExposureDates { get; set; }
        public Currency ReportingCurrency { get; set; }
        public bool ExpensiveFuturesSimulation { get; set; }
        public Dictionary<string, string> FuturesMappingTable { get; set; } = new Dictionary<string, string>();
        public McModelType McModelType { get; set; }
        public PFERegressorType PfeRegressorType { get; set; }
        public bool Parallelize { get; set; }
        public bool DebugMode { get; set; }
        public bool AveragePathCorrection { get; set; }
        public bool CompactMemoryMode { get; set; }
        public bool AvoidRegressionForBackPricing { get; set; }
        public BaseMetric Metric { get; set; } = BaseMetric.PV;
        public double ConfidenceInterval { get; set; } = 0.95;
        public HazzardCurve CreditCurve { get; set; }
        public double CounterpartyRiskWeighting { get; set; }
        public Dictionary<string, string> AssetIdToHedgeGroupMap { get; set; }
        public IIrCurve FundingCurve { get; set; }
        public IIrCurve BaseDiscountCurve { get; set; }
    }
}
