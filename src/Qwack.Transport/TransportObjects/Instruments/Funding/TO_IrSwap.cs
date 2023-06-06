using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_IrSwap 
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();

        public double Notional { get; set; }
        public double ParRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int NDates { get; set; }
        public DateTime[] ResetDates { get; set; }
        public string Currency { get; set; }
        public TO_GenericSwapLeg FixedLeg { get; set; }
        public TO_GenericSwapLeg FloatLeg { get; set; }
        public TO_CashFlowSchedule FlowScheduleFixed { get; set; }
        public TO_CashFlowSchedule FlowScheduleFloat { get; set; }
        public DayCountBasis BasisFixed { get; set; }
        public DayCountBasis BasisFloat { get; set; }
        public string ResetFrequency { get; set; }
        public string SwapTenor { get; set; }
        public SwapPayReceiveType SwapType { get; set; }
        public string ForecastCurve { get; set; }
        public string DiscountCurve { get; set; }
        public string SolveCurve { get; set; }
        public DateTime PillarDate { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public TO_FloatRateIndex RateIndex { get; set; }
        public string PortfolioName { get; set; }
        public string HedgingSet { get; set; }

    }
}
