using System;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments
{
    public class CashFlow
    {
        public CashFlow()
        {
        }

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

        public CashFlow Clone() => new CashFlow
        {
            AccrualPeriodStart = AccrualPeriodStart,
            AccrualPeriodEnd = AccrualPeriodEnd,
            SettleDate = SettleDate,
            ResetDateStart = ResetDateStart,
            ResetDateEnd = ResetDateEnd,

            FixingDateStart = FixingDateStart,
            FixingDateEnd = FixingDateEnd,
            Fv = Fv,
            Pv = Pv,
            Notional = Notional,
            NotionalByYearFraction = NotionalByYearFraction,
            Currency = Currency,
            FixedRateOrMargin = FixedRateOrMargin,
            FlowType = FlowType
        };
    }

    public static class CashflowEx
    {
        public static double GetFloatRate(this CashFlow cashFlow, IIrCurve curve, DayCountBasis basis) => curve.GetForwardRate(cashFlow.AccrualPeriodStart, cashFlow.AccrualPeriodEnd, RateType.Linear, basis);
    }
}
