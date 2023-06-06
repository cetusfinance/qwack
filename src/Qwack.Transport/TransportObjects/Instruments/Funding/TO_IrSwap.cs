using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_IrSwap 
    {
        [ProtoMember(1)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();

        [ProtoMember(2)]
        public double Notional { get; set; }
        [ProtoMember(3)]
        public double ParRate { get; set; }
        [ProtoMember(4)]
        public DateTime StartDate { get; set; }
        [ProtoMember(5)]
        public DateTime EndDate { get; set; }
        [ProtoMember(6)]
        public int NDates { get; set; }
        [ProtoMember(7)]
        public DateTime[] ResetDates { get; set; }
        [ProtoMember(8)]
        public string Currency { get; set; }
        [ProtoMember(9)]
        public TO_GenericSwapLeg FixedLeg { get; set; }
        [ProtoMember(10)]
        public TO_GenericSwapLeg FloatLeg { get; set; }
        [ProtoMember(11)]
        public TO_CashFlowSchedule FlowScheduleFixed { get; set; }
        [ProtoMember(12)]
        public TO_CashFlowSchedule FlowScheduleFloat { get; set; }
        [ProtoMember(13)]
        public DayCountBasis BasisFixed { get; set; }
        [ProtoMember(14)]
        public DayCountBasis BasisFloat { get; set; }
        [ProtoMember(15)]
        public string ResetFrequency { get; set; }
        [ProtoMember(16)]
        public string SwapTenor { get; set; }
        [ProtoMember(17)]
        public SwapPayReceiveType SwapType { get; set; }
        [ProtoMember(18)]
        public string ForecastCurve { get; set; }
        [ProtoMember(19)]
        public string DiscountCurve { get; set; }
        [ProtoMember(20)]
        public string SolveCurve { get; set; }
        [ProtoMember(21)]
        public DateTime PillarDate { get; set; }
        [ProtoMember(22)]
        public string TradeId { get; set; }
        [ProtoMember(23)]
        public string Counterparty { get; set; }
        [ProtoMember(24)]
        public TO_FloatRateIndex RateIndex { get; set; }
        [ProtoMember(25)]
        public string PortfolioName { get; set; }
        [ProtoMember(26)]
        public string HedgingSet { get; set; }

    }
}
