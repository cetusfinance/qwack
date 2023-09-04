using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class EquityBasket : ITrsUnderlying
    {
        public EquityBasket() { }
        public EquityBasket(TO_EquityBasket to, ICurrencyProvider currencyProvider) 
        { 
            Weights = new(to.Weights);
            Notional = to.Notional;
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            FxConversionType = to.FxConversionType;
            Name = to.Name;
            MetaData = new(to.MetaData);
        }

        public Dictionary<string,double> Weights { get; set; }
        public double Notional { get; }
        public FxConversionType FxConversionType { get; set; }
        public string[] AssetIds => Weights.Keys.ToArray();
        public Currency PaymentCurrency => Currency;
        public string Name { get; set; }
        public Currency Currency { get; set; }
        public Dictionary<string, string> MetaData { get ; set; }

        public TO_EquityBasket ToTransportObject() => new()
        {
            Weights = new(Weights),
            Notional = Notional,
            Currency = Currency.Ccy,
            FxConversionType = FxConversionType,
            Name = Name,
            MetaData = new(MetaData)
        };
    }
}
