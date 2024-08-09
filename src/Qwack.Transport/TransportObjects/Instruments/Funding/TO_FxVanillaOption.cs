using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_FxVanillaOption 
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
        public DateTime DeliveryDate { get; set; }
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

        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
        [ProtoMember(101)]
        public OptionExerciseType ExerciseType { get; set; }
        [ProtoMember(102)]
        public double Premium { get; set; }
        [ProtoMember(103)]
        public DateTime PremiumDate { get; set; }
        [ProtoMember(104)]
        public DateTime ExpiryDate { get; set; }
    }
}
