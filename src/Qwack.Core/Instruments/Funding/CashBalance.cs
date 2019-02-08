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
    public class CashBalance : IFundingInstrument, IAssetInstrument
    {
        public CashBalance() { }

        public CashBalance(Currency currency, double notional, DateTime? payDate = null)
        {
            Notional = notional;
            Currency = currency;
            PayDate = payDate ?? DateTime.MinValue;
        }
        
        public double Notional { get; set; }
        public string PortfolioName { get; set; }
        public Currency Currency { get; set; }        
  
        public string SolveCurve { get; set; }
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public DateTime PillarDate { get; set; }
        public DateTime PayDate { get; set; }
        public DateTime LastSensitivityDate => DateTime.MinValue;

        public string[] AssetIds => new string[0];
        public Currency PaymentCurrency => Currency;

        public double Pv(IFundingModel model, bool updateState) => PayDate == DateTime.MinValue || PayDate <= model.BuildDate ? Notional : model.GetDf(Currency, model.BuildDate, PayDate) * Notional;

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

        public string[] IrCurves(IAssetFxModel model) => new[] { model.FundingModel.FxMatrix.DiscountCurveMap[Currency] };
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;
        public string FxPair(IAssetFxModel model) => string.Empty;
        IAssetInstrument IAssetInstrument.Clone() => (IAssetInstrument)Clone();
        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();
    }
}
