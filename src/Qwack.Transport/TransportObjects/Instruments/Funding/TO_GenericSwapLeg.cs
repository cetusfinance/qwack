using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_GenericSwapLeg
    {
       
        public string Currency { get; set; }
        public DateTime EffectiveDate { get; set; }
        public TO_ITenorDate TerminationDate { get; set; }
        public string FixingCalendar { get; set; }
        public string ResetCalendar { get; set; }
        public string AccrualCalendar { get; set; }
        public string PaymentCalendar { get; set; }
        public RollType ResetRollType { get; set; } = RollType.ModFollowing;
        public RollType PaymentRollType { get; set; } = RollType.Following;
        public RollType FixingRollType { get; set; } = RollType.Previous;
        public string RollDay { get; set; } = "Termination";
        public StubType StubType { get; set; } = StubType.ShortFront;
        public string ResetFrequency { get; set; }
        public string FixingOffset { get; set; } = "2b";
        public string ForecastTenor { get; set; }
        public SwapLegType LegType { get; set; }
        public string PaymentOffset { get; set; } = "0b";
        public OffsetRelativeToType PaymentOffsetRelativeTo { get; set; } = OffsetRelativeToType.PeriodEnd;
        public decimal FixedRateOrMargin { get; set; }
        public decimal Nominal { get; set; } = 1e6M;
        public DayCountBasis AccrualDCB { get; set; }
        public FraDiscountingType FraDiscounting { get; set; }
        public AverageType AveragingType { get; set; }
        public ExchangeType NotionalExchange { get; set; }
        public SwapPayReceiveType Direction { get; set; }
    }
}
