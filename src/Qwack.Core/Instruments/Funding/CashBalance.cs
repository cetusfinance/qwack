using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class CashBalance : IFundingInstrument, IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public CashBalance() { }

        public CashBalance(Currency currency, double notional, DateTime? payDate = null) : base()
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

        public string[] AssetIds => Array.Empty<string>();
        public Currency PaymentCurrency => Currency;

        public double Pv(IFundingModel model, bool updateState) => PayDate == DateTime.MinValue || PayDate <= model.BuildDate ? Notional : model.GetDf(Currency, model.BuildDate, PayDate) * Notional;

        public double FlowsT0(IFundingModel model) => 0.0;

        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model) => throw new NotImplementedException();

        public override bool Equals(object obj) => obj is CashBalance balance &&
                   Notional == balance.Notional &&
                   EqualityComparer<Currency>.Default.Equals(Currency, balance.Currency) &&
                   TradeId == balance.TradeId;

        public List<string> Dependencies(IFxMatrix matrix) => new();

        public double CalculateParRate(IFundingModel model) => 0.0;

        public IFundingInstrument Clone() => new CashBalance
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

        public IFundingInstrument SetParRate(double parRate) => Clone();

        public string[] IrCurves(IAssetFxModel model) => new[] { model.FundingModel.FxMatrix.DiscountCurveMap[Currency] };
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new();
        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;
        public string FxPair(IAssetFxModel model) => string.Empty;
        IAssetInstrument IAssetInstrument.Clone() => (IAssetInstrument)Clone();
        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new()
        { new CashFlow()
                {
                    Currency = Currency,
                    SettleDate = PayDate==DateTime.MinValue ? model.BuildDate:PayDate,
                    Notional = Notional,
                    Fv = Notional
                }
        };

        public double SuggestPillarValue(IFundingModel model) => 0.05;
    }
}
