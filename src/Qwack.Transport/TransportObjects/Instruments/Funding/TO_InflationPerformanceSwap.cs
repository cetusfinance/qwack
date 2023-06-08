using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_InflationPerformanceSwap
    {
        public TO_InflationPerformanceSwap() { }

        [ProtoMember(1)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        [ProtoMember(2)]
        public double FixedFlow { get; set; }
        [ProtoMember(3)]
        public double T { get; set; }
        [ProtoMember(4)]
        public double Notional { get; set; }
        [ProtoMember(5)]
        public double ParRate { get; set; }
        [ProtoMember(6)]
        public DateTime StartDate { get; set; }
        [ProtoMember(7)]
        public DateTime EndDate { get; set; }
        [ProtoMember(8)]
        public double InitialFixing { get; set; }
        [ProtoMember(9)]
        public DateTime[] ResetDates { get; set; }
        [ProtoMember(10)]
        public string Currency { get; set; }
        [ProtoMember(11)]
        public DayCountBasis BasisFixed { get; set; }
        [ProtoMember(12)]
        public DayCountBasis BasisFloat { get; set; }
        [ProtoMember(13)]
        public string ResetFrequency { get; set; }
        [ProtoMember(14)]
        public string SwapTenor { get; set; }
        [ProtoMember(15)]
        public SwapPayReceiveType SwapType { get; set; }
        [ProtoMember(16)]
        public string ForecastCurveCpi { get; set; }
        [ProtoMember(17)]
        public string DiscountCurve { get; set; }
        [ProtoMember(18)]
        public string SolveCurve { get; set; }
        [ProtoMember(19)]
        public DateTime PillarDate { get; set; }
        [ProtoMember(20)]
        public string TradeId { get; set; }
        [ProtoMember(21)]
        public string Counterparty { get; set; }
        [ProtoMember(22)]
        public TO_InflationIndex RateIndex { get; set; }
        [ProtoMember(23)]
        public string PortfolioName { get; set; }
        [ProtoMember(24)]
        public string HedgingSet { get; set; }
        [ProtoMember(25)]
        public DateTime? SettleDate { get; set; }
    }
}
