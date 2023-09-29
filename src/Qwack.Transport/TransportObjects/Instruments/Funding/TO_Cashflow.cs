using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_Cashflow
    {
        [ProtoMember(1)]
        public DateTime AccrualPeriodStart { get; set; }
        [ProtoMember(2)]
        public DateTime AccrualPeriodEnd { get; set; }
        [ProtoMember(3)]
        public DateTime SettleDate { get; set; }
        [ProtoMember(4)]
        public DateTime ResetDateStart { get; set; }
        [ProtoMember(5)]
        public DateTime ResetDateEnd { get; set; }
        [ProtoMember(6)]
        public DateTime FixingDateStart { get; set; }
        [ProtoMember(7)]
        public DateTime FixingDateEnd { get; set; }
        [ProtoMember(8)]
        public double Fv { get; set; }
        [ProtoMember(9)]
        public double Pv { get; set; }
        [ProtoMember(10)]
        public double Notional { get; set; }
        [ProtoMember(11)]
        public double YearFraction { get; set; }
        [ProtoMember(12)]
        public double Dcf { get; set; }
        [ProtoMember(13)]
        public string Currency { get; set; }
        [ProtoMember(14)]
        public double FixedRateOrMargin { get; set; }
        [ProtoMember(15)]
        public int CpiFixingLagInMonths { get; set; }
        [ProtoMember(16)]
        public FlowType FlowType { get; set; }
        [ProtoMember(17)]
        public DayCountBasis Basis { get; set; }
        [ProtoMember(18)]
        public TO_FloatRateIndex RateIndex { get; set; }
        [ProtoMember(19)]
        public string AssetId { get; set; }
        [ProtoMember(20)]
        public double? InitialFixing { get; set; }
    }
}
