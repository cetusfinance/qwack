using System.Collections.Generic;
using System.Data;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class EquityIndex : ITrsUnderlying
    {
        public EquityIndex() { }
        public EquityIndex(TO_EquityIndex to, ICurrencyProvider currencyProvider) 
        { 
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            FxConversionType = to.FxConversionType;
            Name = to.Name;
            MetaData = to.MetaData == null ? null : new(to.MetaData);
            AssetId = to.AssetId;
        }

        public FxConversionType FxConversionType { get; set; }
        public string AssetId { get; set; }
        public string[] AssetIds => new[] { AssetId } ;
        public Currency PaymentCurrency => Currency;
        public string Name { get; set; }
        public Currency Currency { get; set; }
        public Dictionary<string, string> MetaData { get ; set; }

        public TO_ITrsUnderlying ToTransportObject() => new()
        {
            EquityIndex = new TO_EquityIndex()
            {
                Currency = Currency?.Ccy,
                FxConversionType = FxConversionType,
                Name = Name,
                MetaData = MetaData == null ? null : new(MetaData),
                AssetId = AssetId
            }
        };
    }
}
