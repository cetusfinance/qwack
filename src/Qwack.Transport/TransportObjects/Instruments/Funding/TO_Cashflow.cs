using System;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_Cashflow
    {

        public DateTime AccrualPeriodStart { get; set; }
        public DateTime AccrualPeriodEnd { get; set; }
        public DateTime SettleDate { get; set; }
        public DateTime ResetDateStart { get; set; }
        public DateTime ResetDateEnd { get; set; }
        public DateTime FixingDateStart { get; set; }
        public DateTime FixingDateEnd { get; set; }

        public double Fv { get; set; }
        public double Pv { get; set; }
        public double Notional { get; set; }
        public double YearFraction { get; set; }
        public double Dcf { get; set; }
        public string Currency { get; set; }
        public double FixedRateOrMargin { get; set; }
        public int CpiFixingLagInMonths { get; set; }

        public FlowType FlowType { get; set; }
        public DayCountBasis Basis { get; set; }
        public TO_FloatRateIndex RateIndex { get; set; }
    }
}
