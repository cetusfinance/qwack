using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class FixedRateLoanDeposit : IFundingInstrument
    {
        public FixedRateLoanDeposit() { }

        public FixedRateLoanDeposit(DateTime startDate, DateTime endDate, double interestRate, Currency currency, DayCountBasis basis, double notional, string discountCurve)
        {
            StartDate = startDate;
            EndDate = endDate;
            InterestRate = interestRate;
            Basis = basis;
            Notional = notional;
            DiscountCurve = discountCurve;
            Currency = currency;

            LoanDepoSchedule = new CashFlowSchedule { Flows = new List<CashFlow>() };
            LoanDepoSchedule.Flows.Add(new CashFlow
            {
                Notional = Notional,
                Currency = Currency,
                FlowType = FlowType.FixedAmount,
                SettleDate = StartDate,
            });
            LoanDepoSchedule.Flows.Add(new CashFlow
            {
                Notional = -Notional,
                Currency = Currency,
                FlowType = FlowType.FixedAmount,
                SettleDate = EndDate,
            });
            var dcf = StartDate.CalculateYearFraction(EndDate, Basis);
            LoanDepoSchedule.Flows.Add(new CashFlow
            {
                Notional = -Notional,
                Currency = Currency,
                FlowType = FlowType.FixedRate,
                SettleDate = EndDate,
                AccrualPeriodStart = StartDate,
                AccrualPeriodEnd = EndDate,
                FixedRateOrMargin = InterestRate,
                NotionalByYearFraction = dcf,
                Fv = -Notional * dcf * interestRate
            });
        }

        public double Notional { get; set; }
        public double InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Currency Currency { get; set; }
        public CashFlowSchedule LoanDepoSchedule { get; set; }
        public DayCountBasis Basis { get; set; }
        public string DiscountCurve { get; set; }

        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public double Pv(IFundingModel model, bool updateState)
        {
            var discountCurve = model.Curves[DiscountCurve];
            var pv = LoanDepoSchedule.PV(discountCurve, discountCurve, updateState, true, false, DayCountBasis.ACT360, model.BuildDate);
            return pv;
        }

        public double FlowsT0(IFundingModel model)
        {
            if (StartDate == model.BuildDate)
            {
                return Notional;
            }
            else if (EndDate == model.BuildDate)
            {
                var dcf = StartDate.CalculateYearFraction(EndDate, Basis);
                return -Notional - Notional * dcf * InterestRate;
            }
            return 0.0;
        }

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model)
        {
            Pv(model, true);
            return LoanDepoSchedule;
        }

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is FixedRateLoanDeposit deposit &&
                   Notional == deposit.Notional &&
                   InterestRate == deposit.InterestRate &&
                   StartDate == deposit.StartDate &&
                   EndDate == deposit.EndDate &&
                   EqualityComparer<Currency>.Default.Equals(Currency, deposit.Currency) &&
                   Basis == deposit.Basis &&
                   DiscountCurve == deposit.DiscountCurve &&
                   TradeId == deposit.TradeId;

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double CalculateParRate(IFundingModel model) => 0.0;

        public IFundingInstrument Clone() => new FixedRateLoanDeposit
        {
            Basis = Basis,
            Currency = Currency,
            Counterparty = Counterparty,
            DiscountCurve = DiscountCurve,
            EndDate = EndDate,
            InterestRate = InterestRate,
            LoanDepoSchedule = LoanDepoSchedule.Clone(),
            Notional = Notional,
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            StartDate = StartDate,
            TradeId = TradeId
        };

        public IFundingInstrument SetParRate(double parRate) => new FixedRateLoanDeposit(StartDate, EndDate, parRate, Currency, Basis, Notional, DiscountCurve);
    }
}
