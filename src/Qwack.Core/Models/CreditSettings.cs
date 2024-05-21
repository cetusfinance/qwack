using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

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

        public CreditSettings () { }
        public CreditSettings (TO_CreditSettings to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            Metric = to.Metric;
            ConfidenceInterval = to.ConfidenceInterval;
            CreditCurve = new HazzardCurve(to.CreditCurve);
            LGD = to.LGD;
            CounterpartyRiskWeighting = to.CounterpartyRiskWeighting;
            AssetIdToHedgeGroupMap = new(to.AssetIdToHedgeGroupMap);
            FundingCurve = IrCurveFactory.GetCurve(to.FundingCurve, currencyProvider, calendarProvider);
            BaseDiscountCurve = IrCurveFactory.GetCurve(to.BaseDiscountCurve, currencyProvider, calendarProvider);
            PfeRegressorType = to.PfeRegressorType;
            ExposureDates = to.ExposureDates;
        }

        public CreditSettings Clone() => new()
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

        public TO_CreditSettings GetTransportObject() => new()
        {
            Metric = Metric,
            ConfidenceInterval = ConfidenceInterval,
            CreditCurve = CreditCurve.GetTransportObject(),
            LGD = LGD,
            CounterpartyRiskWeighting = CounterpartyRiskWeighting,
            AssetIdToHedgeGroupMap = AssetIdToHedgeGroupMap,
            FundingCurve = FundingCurve.GetTransportObject(),
            BaseDiscountCurve = BaseDiscountCurve.GetTransportObject(),
            PfeRegressorType = PfeRegressorType,
            ExposureDates = ExposureDates
        };
    }
}
