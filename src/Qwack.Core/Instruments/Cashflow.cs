using System;
using Qwack.Core.Basic;

namespace Qwack.Core.Instruments
{
    public class CashFlow
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
        public double NotionalByYearFraction { get; set; }
        public Currency Currency { get; set; }
        public double FixedRateOrMargin { get; set; }

        public FlowType FlowType { get; set; }
    }
}
