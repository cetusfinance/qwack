using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_GenericSwapLeg
    {
        [ProtoMember(1)]
       
        public string Currency { get; set; }
        [ProtoMember(2)]
        public DateTime EffectiveDate { get; set; }
        [ProtoMember(3)]
        public TO_ITenorDate TerminationDate { get; set; }
        [ProtoMember(4)]
        public string FixingCalendar { get; set; }
        [ProtoMember(5)]
        public string ResetCalendar { get; set; }
        [ProtoMember(6)]
        public string AccrualCalendar { get; set; }
        [ProtoMember(7)]
        public string PaymentCalendar { get; set; }
        [ProtoMember(8)]
        public RollType ResetRollType { get; set; } = RollType.ModFollowing;
        [ProtoMember(9)]       
        public RollType PaymentRollType { get; set; } = RollType.Following;
        [ProtoMember(10)]
        public RollType FixingRollType { get; set; } = RollType.Previous;
        [ProtoMember(11)]
        public string RollDay { get; set; } = "Termination";
        [ProtoMember(12)]
        public StubType StubType { get; set; } = StubType.ShortFront;
        [ProtoMember(13)]
        public string ResetFrequency { get; set; }
        [ProtoMember(14)]
        public string FixingOffset { get; set; } = "2b";
        [ProtoMember(15)]
        public string ForecastTenor { get; set; }
        [ProtoMember(16)]
        public SwapLegType LegType { get; set; }
        [ProtoMember(17)]
        public string PaymentOffset { get; set; } = "0b";
        [ProtoMember(18)]
        public OffsetRelativeToType PaymentOffsetRelativeTo { get; set; } = OffsetRelativeToType.PeriodEnd;
        [ProtoMember(19)]
        public decimal FixedRateOrMargin { get; set; }
        [ProtoMember(20)]
        public decimal Nominal { get; set; } = 1e6M;
        [ProtoMember(21)]
        public DayCountBasis AccrualDCB { get; set; }
        [ProtoMember(22)]
        public FraDiscountingType FraDiscounting { get; set; }
        [ProtoMember(23)]
        public AverageType AveragingType { get; set; }
        [ProtoMember(24)]
        public ExchangeType NotionalExchange { get; set; }
        [ProtoMember(25)]
        public SwapPayReceiveType Direction { get; set; }
        [ProtoMember(26)]
        public TrsLegType? TrsLegType { get; set; }
        [ProtoMember(27)]
        public string AssetId { get; set; }
        [ProtoMember(28)]
        public double? InitialFixing { get; set; }
    }
}
