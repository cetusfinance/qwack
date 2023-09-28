using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    internal static class TrsUnderlyingFactory
    {
        internal static ITrsUnderlying FromTo(this TO_ITrsUnderlying to, ICurrencyProvider currencyProvider)
        {
            if (to?.EquityBasket != null)
            {
                return new EquityBasket(to.EquityBasket, currencyProvider);
            }

            if(to?.EquityIndex != null)
            {
                return new EquityIndex(to.EquityIndex, currencyProvider);
            }

            return null;
        }
    }
}
