using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_FuturesOption
    {
        [ProtoMember(1)]
        public string TradeId { get; set; }
        [ProtoMember(2)]
        public string Counterparty { get; set; }
        [ProtoMember(3)]
        public string PortfolioName { get; set; }
        [ProtoMember(4)]
        public double ContractQuantity { get; set; }
        [ProtoMember(5)]
        public double LotSize { get; set; }
        [ProtoMember(6)]
        public double PriceMultiplier { get; set; }
        [ProtoMember(7)]
        public TradeDirection Direction { get; set; }
        [ProtoMember(8)]
        public DateTime ExpiryDate { get; set; }
        [ProtoMember(9)]
        public double Strike { get; set; }
        [ProtoMember(10)]
        public string AssetId { get; set; }
        [ProtoMember(11)]
        public string Currency { get; set; }
        [ProtoMember(12)]
        public Dictionary<string, string> MetaData { get; set; }
        [ProtoMember(13)]
        public bool? IsPerpetual { get; set; }

        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
        [ProtoMember(101)]
        public OptionExerciseType ExerciseType { get; set; }
        [ProtoMember(102)]
        public OptionMarginingType MarginingType { get; set; }
        [ProtoMember(103)]
        public string DiscountCurve { get; set; }
        [ProtoMember(104)]
        public double Premium { get; set; }
        [ProtoMember(105)]
        public DateTime PremiumDate { get; set; }
    }
}
