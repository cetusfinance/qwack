using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

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

        public override bool Equals(object obj)
        {
            var wrapper = obj as CashWrapper;
            if (obj == null)
                return obj.Equals(UnderlyingInstrument);
            else
            {
                switch(wrapper.UnderlyingInstrument)
                {
                    case AsianOption ao:
                        return ao.Equals(UnderlyingInstrument);
                    case AsianSwap asw:
                        return asw.Equals(UnderlyingInstrument);
                    case AsianSwapStrip ass:
                        return ass.Equals(UnderlyingInstrument);
                    case AsianBasisSwap abs:
                        return abs.Equals(UnderlyingInstrument);
                    case EuropeanBarrierOption bo:
                        return bo.Equals(UnderlyingInstrument);
                    case EuropeanOption eu:
                        return eu.Equals(UnderlyingInstrument);
                    case Forward f:
                        return f.Equals(UnderlyingInstrument);
                    case FxVanillaOption feu:
                        return feu.Equals(UnderlyingInstrument);
                    case FxForward fxf:
                        return fxf.Equals(UnderlyingInstrument);
                    case FuturesOption futo:
                        return futo.Equals(UnderlyingInstrument);
                    case Future fut:
                        return fut.Equals(UnderlyingInstrument);
                    case BackPricingOption bpo:
                        return bpo.Equals(UnderlyingInstrument);
                    case MultiPeriodBackpricingOption mbpo:
                        return mbpo.Equals(UnderlyingInstrument);
                    case AsianLookbackOption lbo:
                        return lbo.Equals(UnderlyingInstrument);
                }
                return wrapper.UnderlyingInstrument.Equals(UnderlyingInstrument);
            }
                
        }       

        public string FxPair(IAssetFxModel model) => UnderlyingInstrument.FxPair(model);
        public FxConversionType FxType(IAssetFxModel model) => UnderlyingInstrument.FxType(model);
        public string[] IrCurves(IAssetFxModel model) => UnderlyingInstrument.IrCurves(model);
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => UnderlyingInstrument.PastFixingDates(valDate);
        public IAssetInstrument SetStrike(double strike)=> UnderlyingInstrument.SetStrike(strike);

        
    }
}
