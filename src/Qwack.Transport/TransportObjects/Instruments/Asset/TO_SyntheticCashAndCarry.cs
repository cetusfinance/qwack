using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_SyntheticCashAndCarry 
    {
        [ProtoMember(1)]
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        [ProtoMember(2)]
        public string TradeId { get; set; }
        [ProtoMember(3)]
        public string Counterparty { get; set; }
        [ProtoMember(4)]
        public string PortfolioName { get; set; }
        [ProtoMember(5)]
        public string HedgingSet { get; set; }

        [ProtoMember(6)]
        public TO_Forward NearLeg { get; set; }
        [ProtoMember(7)]
        public TO_Forward FarLeg { get; set; }

        [ProtoMember(8)]
        public DayCountBasis FundingBasis { get; set; }
        [ProtoMember(9)]
        public RateType FundingRateType { get; set; } = RateType.Linear;
        [ProtoMember(10)]
        public double FundingRate { get; set; }

        [ProtoMember(11)]
        public string DiscountCurve { get; set; }
        [ProtoMember(12)]
        public string SpotLag { get; set; }


    }
}
