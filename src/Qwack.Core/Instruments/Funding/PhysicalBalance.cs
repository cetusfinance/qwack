namespace Qwack.Core.Instruments.Funding
{
    public class PhysicalBalance : CashBalance
    {
        public PhysicalBalance() : base() { }

        public new IFundingInstrument Clone() => new PhysicalBalance
        {
            Currency = Currency,
            Counterparty = Counterparty,
            Notional = Notional,
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            TradeId = TradeId,
            PortfolioName = PortfolioName,
            PayDate = PayDate
        };


    }
}
