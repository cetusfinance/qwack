using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments
{
    public class CashWrapper : IAssetInstrument
    {
        public CashWrapper(IAssetInstrument underlyingInstrument, List<CashBalance> cashBalances = null)
        {
            UnderlyingInstrument = underlyingInstrument;
            CashBalances = cashBalances ?? new List<CashBalance>();
        }

        public IAssetInstrument UnderlyingInstrument { get; }
        public List<CashBalance> CashBalances { get; }

        public string[] AssetIds => UnderlyingInstrument.AssetIds;
        public Currency Currency => UnderlyingInstrument.Currency;
        public Currency PaymentCurrency => UnderlyingInstrument.PaymentCurrency;
        public string TradeId => UnderlyingInstrument.TradeId;
        public string Counterparty { get => UnderlyingInstrument.Counterparty; set => UnderlyingInstrument.Counterparty = value; }
        public string PortfolioName { get => UnderlyingInstrument.PortfolioName; set => UnderlyingInstrument.PortfolioName = value; }
        public DateTime LastSensitivityDate => UnderlyingInstrument.LastSensitivityDate;
        public IAssetInstrument Clone() => new CashWrapper(UnderlyingInstrument, new List<CashBalance>(CashBalances.Select(x => (CashBalance)x.Clone())));
        public string FxPair(IAssetFxModel model) => UnderlyingInstrument.FxPair(model);
        public FxConversionType FxType(IAssetFxModel model) => UnderlyingInstrument.FxType(model);
        public string[] IrCurves(IAssetFxModel model) => UnderlyingInstrument.IrCurves(model);
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => UnderlyingInstrument.PastFixingDates(valDate);
        public IAssetInstrument SetStrike(double strike)=> UnderlyingInstrument.SetStrike(strike);

        
    }
}
