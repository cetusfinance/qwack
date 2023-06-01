using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_InflationFwd
    {
        [ProtoMember(1)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        [ProtoMember(2)]
        public double Notional { get; set; }
        [ProtoMember(3)]
        public double Strike { get; set; }
        [ProtoMember(4)]
        public DateTime FixingDate { get; set; }
        [ProtoMember(5)]
        public double InitialFixing { get; set; }
        [ProtoMember(6)]
        public DateTime[] ResetDates { get; set; }
        [ProtoMember(7)]
        public string Currency { get; set; }
        [ProtoMember(8)]
        public string ForecastCurveCpi { get; set; }
        [ProtoMember(9)]
        public string DiscountCurve { get; set; }
        [ProtoMember(10)]
        public string SolveCurve { get; set; }
        [ProtoMember(11)]
        public DateTime PillarDate { get; set; }
        [ProtoMember(12)]
        public string TradeId { get; set; }
        [ProtoMember(13)]
        public string Counterparty { get; set; }
        [ProtoMember(14)]
        public TO_InflationIndex RateIndex { get; set; }
        [ProtoMember(15)]
        public string PortfolioName { get; set; }
    }
}
