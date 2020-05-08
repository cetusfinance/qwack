using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Core.Basic
{
    public class VolSurfaceKey
    {
        public VolSurfaceKey() { }
        public VolSurfaceKey(string assetId, Currency currency):base()
        {
            AssetId = assetId;
            Currency = currency;
        }

        public VolSurfaceKey(TO_VolSurfaceKey transportObject, ICurrencyProvider currencyProvider) 
            : this(transportObject.AssetId, currencyProvider.GetCurrencySafe(transportObject.Currency))
        { 
        }


        public string AssetId { get; set; }

        public Currency Currency { get; set; }

        public override bool Equals(object obj) => obj is VolSurfaceKey key &&
                   AssetId == key.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(Currency, key.Currency);

        public override string ToString() => AssetId + "~" + Currency?.Ccy;

        public override int GetHashCode()
        {
            var hashCode = 159653148;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AssetId);
            hashCode = hashCode * -1521134295 + EqualityComparer<Currency>.Default.GetHashCode(Currency);
            return hashCode;
        }

        public TO_VolSurfaceKey GetTransportObject() =>
            new TO_VolSurfaceKey
            {
                AssetId = AssetId,
                Currency = Currency?.Ccy
            };
    }
}
