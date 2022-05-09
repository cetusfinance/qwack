using System;
using System.Collections.Generic;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Core.Basic
{
    public class VolSurfaceKey
    {
        const char MagicChar = '¬';

        public VolSurfaceKey() { }
        public VolSurfaceKey(string assetId, Currency currency) : base()
        {
            AssetId = assetId;
            Currency = currency;
        }

        public VolSurfaceKey(TO_VolSurfaceKey transportObject, ICurrencyProvider currencyProvider)
            : this(transportObject.AssetId, currencyProvider.GetCurrencySafe(transportObject.Currency))
        {
        }

        public VolSurfaceKey(string keyAsString, ICurrencyProvider currencyProvider)
            : this(keyAsString.Split(MagicChar)[0], keyAsString.Split(MagicChar).Length > 1 ? currencyProvider.GetCurrencySafe(keyAsString.Split(MagicChar)[1]) : null)
        {
        }


        public string AssetId { get; set; }

        public Currency Currency { get; set; }

        public override bool Equals(object obj) => obj is VolSurfaceKey key &&
                   AssetId == key.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(Currency, key.Currency);

        public override string ToString() => AssetId + "~" + Currency?.Ccy;

        public override int GetHashCode() => HashCode.Combine(AssetId, Currency);

        public string GetTransportObject() => AssetId + MagicChar + Currency?.Ccy;
    }
}
