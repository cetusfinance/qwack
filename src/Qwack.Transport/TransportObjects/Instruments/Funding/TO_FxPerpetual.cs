using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_FxPerpetual
    {
        [ProtoMember(1)]
        public string TradeId { get; set; }
        [ProtoMember(2)]
        public string Counterparty { get; set; }
        [ProtoMember(3)]
        public string PortfolioName { get; set; }
        [ProtoMember(4)]
        public double Strike { get; set; }
        [ProtoMember(5)]
        public double DomesticQuantity { get; set; }
        [ProtoMember(6)]
        public double FundingRateHourly { get; set; }
        [ProtoMember(7)]
        public string DomesticCCY { get; set; }
        [ProtoMember(8)]
        public string ForeignCCY { get; set; }
        [ProtoMember(9)]
        public string ForeignDiscountCurve { get; set; }
        [ProtoMember(10)]
        public string SolveCurve { get; set; }
        [ProtoMember(11)]
        public DateTime PillarDate { get; set; }
        [ProtoMember(12)]
        public string HedgingSet { get; set; }
        [ProtoMember(13)]
        public Dictionary<string, string> MetaData { get; set; }
    }
}
