using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public class CreditSettings
    {
        public BaseMetric Metric { get; set; } = BaseMetric.PV;
        public double ConfidenceInterval { get; set; } = 0.95;
        public HazzardCurve CreditCurve { get; set; }
        public double LGD { get; set; }
        public double CounterpartyRiskWeighting { get; set; }
        public Dictionary<string, string> AssetIdToHedgeGroupMap { get; set; }
        public IIrCurve FundingCurve { get; set; }
        public IIrCurve BaseDiscountCurve { get; set; }
        public PFERegressorType PfeRegressorType { get; set; }
        public DateTime[] ExposureDates { get; set; }

        public CreditSettings Clone() => new CreditSettings
        {
            Metric = Metric,
            ConfidenceInterval = ConfidenceInterval,
            CreditCurve = CreditCurve,
            LGD = LGD,
            CounterpartyRiskWeighting = CounterpartyRiskWeighting,
            AssetIdToHedgeGroupMap = AssetIdToHedgeGroupMap,
            FundingCurve = FundingCurve,
            BaseDiscountCurve = BaseDiscountCurve,
            PfeRegressorType = PfeRegressorType,
            ExposureDates = ExposureDates
        };

    }
}
