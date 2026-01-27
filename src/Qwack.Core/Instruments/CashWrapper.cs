using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments
{
    public class CashWrapper : IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public CashWrapper(IAssetInstrument underlyingInstrument, List<CashBalance> cashBalances = null)
        {
            UnderlyingInstrument = underlyingInstrument;
            CashBalances = cashBalances ?? new List<CashBalance>();
            MetaData = underlyingInstrument.MetaData;
        }
        public CashWrapper(TO_CashWrapper to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) :
            this(InstrumentFactory.GetInstrument(to.Underlying, currencyProvider, calendarProvider) as IAssetInstrument, to.CashBalances?.Select(x => new CashBalance(x, currencyProvider)).ToList())
        { 
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
                return wrapper.UnderlyingInstrument switch
                {
                    AsianOption ao => ao.Equals(UnderlyingInstrument),
                    AsianSwap asw => asw.Equals(UnderlyingInstrument),
                    AsianSwapStrip ass => ass.Equals(UnderlyingInstrument),
                    AsianBasisSwap abs => abs.Equals(UnderlyingInstrument),
                    EuropeanBarrierOption bo => bo.Equals(UnderlyingInstrument),
                    EuropeanOption eu => eu.Equals(UnderlyingInstrument),
                    Forward f => f.Equals(UnderlyingInstrument),
                    FxVanillaOption feu => feu.Equals(UnderlyingInstrument),
                    FxForward fxf => fxf.Equals(UnderlyingInstrument),
                    FuturesOption futo => futo.Equals(UnderlyingInstrument),
                    Future fut => fut.Equals(UnderlyingInstrument),
                    BackPricingOption bpo => bpo.Equals(UnderlyingInstrument),
                    MultiPeriodBackpricingOption mbpo => mbpo.Equals(UnderlyingInstrument),
                    AsianLookbackOption lbo => lbo.Equals(UnderlyingInstrument),
                    QPDoubleChoiceOption doubleChoiceOption => doubleChoiceOption.Equals(UnderlyingInstrument),
                    StripQPSwaption stripQPSwaption => stripQPSwaption.Equals(UnderlyingInstrument),
                    _ => wrapper.UnderlyingInstrument.Equals(UnderlyingInstrument),
                };
            }
        }

        public override int GetHashCode() => UnderlyingInstrument.GetHashCode();

        public string FxPair(IAssetFxModel model) => UnderlyingInstrument.FxPair(model);
        public FxConversionType FxType(IAssetFxModel model) => UnderlyingInstrument.FxType(model);
        public string[] IrCurves(IAssetFxModel model) => UnderlyingInstrument.IrCurves(model);
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => UnderlyingInstrument.PastFixingDates(valDate);
        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) => UnderlyingInstrument.PastFixingDatesFx(model, valDate);
        public IAssetInstrument SetStrike(double strike) => UnderlyingInstrument.SetStrike(strike);

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.CashWrapper,
            CashWrapper = new TO_CashWrapper
            {
                CashBalances = CashBalances.Select(x => x.ToTransportObject()).ToArray(),
                Underlying = UnderlyingInstrument.GetTransportObject()
            }
        };
    }
}
