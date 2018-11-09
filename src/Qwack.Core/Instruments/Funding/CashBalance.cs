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
        public CashBalance(Currency currency, double notional)
        {
            Notional = notional;
            Ccy = currency;
        }
        
        public double Notional { get; set; }
    
        public Currency Ccy { get; set; }        
  
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
                   EqualityComparer<Currency>.Default.Equals(Ccy, balance.Ccy) &&
                   TradeId == balance.TradeId;

        public List<string> Dependencies(IFxMatrix matrix) => new List<string>();
    }
}
