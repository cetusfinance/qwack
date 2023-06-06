using System;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

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
        public double YearFraction { get; set; }
        public double Dcf { get; set; }
        public Currency Currency { get; set; }
        public double FixedRateOrMargin { get; set; }
        public int CpiFixingLagInMonths { get; set; }

        public FlowType FlowType { get; set; }
        public DayCountBasis Basis { get; set; }
        public FloatRateIndex RateIndex { get; set; }

        public CashFlow(TO_Cashflow to, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            AccrualPeriodStart = to.AccrualPeriodStart;
            AccrualPeriodEnd = to.AccrualPeriodEnd;
            SettleDate = to.SettleDate;
            ResetDateStart = to.ResetDateStart;
            ResetDateEnd = to.ResetDateEnd;
            FixingDateStart = to.FixingDateStart;
            FixingDateEnd = to.FixingDateEnd;
            Fv = to.Fv;
            Pv = to.Pv;
            Notional = to.Notional;
            YearFraction = to.YearFraction;
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            FixedRateOrMargin = to.FixedRateOrMargin;
            FlowType = to.FlowType;
            Basis = to.Basis;
            RateIndex = new FloatRateIndex(to.RateIndex, calendarProvider, currencyProvider);
            Dcf = to.Dcf;
            CpiFixingLagInMonths = to.CpiFixingLagInMonths;
        }

        public CashFlow Clone() => new()
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
            YearFraction = YearFraction,
            Currency = Currency,
            FixedRateOrMargin = FixedRateOrMargin,
            FlowType = FlowType,
            Basis = Basis,
            RateIndex = RateIndex,
            Dcf = Dcf,
            CpiFixingLagInMonths = CpiFixingLagInMonths
        };

        public TO_Cashflow GetTransportObject() => new ()
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
            YearFraction = YearFraction,
            Currency = Currency,
            FixedRateOrMargin = FixedRateOrMargin,
            FlowType = FlowType,
            Basis = Basis,
            RateIndex = RateIndex.GetTransportObject(),
            Dcf = Dcf,
            CpiFixingLagInMonths = CpiFixingLagInMonths
        };
    }

    public static class CashflowEx
    {
        public static double GetFloatRate(this CashFlow cashFlow, IIrCurve curve, DayCountBasis basis) => curve.GetForwardRate(cashFlow.AccrualPeriodStart, cashFlow.AccrualPeriodEnd, RateType.Linear, basis);
    }
}
