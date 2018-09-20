using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;

namespace Qwack.Core.Instruments
{
    public class Portfolio : IInstrument
    {
        public List<IInstrument> Instruments { get; set; }
    }

    public static class PortfolioEx
    {
        public static DateTime LastSensitivityDate(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return DateTime.MinValue;

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward);

            if (!fxTrades.Any() && !assetTrades.Any())
                return DateTime.MinValue;
            else if (!fxTrades.Any())
                return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate); 
            else if (!assetTrades.Any())
                return fxTrades.Max(x => ((FxForward)x).DeliveryDate);
            return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate).Max(fxTrades.Max(x => ((FxForward)x).DeliveryDate));
        }
    }
}
