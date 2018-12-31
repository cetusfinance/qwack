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
    public class CashBalance : IFundingInstrument
    {
        public CashBalance() { }

        public CashBalance(Currency currency, double notional)
        {
            Notional = notional;
            Currency = currency;
        }
        
        public double Notional { get; set; }
    
        public Currency Currency { get; set; }        
  
        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }

        public DateTime LastSensitivityDate => DateTime.MinValue;

        public double Pv(IFundingModel model, bool updateState) => Notional;

        public double FlowsT0(IFundingModel model) => 0.0;

        public CashFlowSchedule ExpectedCashFlows(IFundingModel model) => new CashFlowSchedule();

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is CashBalance balance &&
                   Notional == balance.Notional &&
                   EqualityComparer<Currency>.Default.Equals(Currency, balance.Currency) &&
                   TradeId == balance.TradeId;

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();

        public double CalculateParRate(IFundingModel model) => 0.0;

        public IFundingInstrument Clone() => new CashBalance
        {
            Currency = Currency,
            Counterparty = Counterparty,
            Notional = Notional,
            PillarDate = PillarDate,
            SolveCurve = SolveCurve,
            TradeId = TradeId
        };

        public IFundingInstrument SetParRate(double parRate) => Clone();
    }
}
