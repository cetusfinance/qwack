using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                .Where(x => x is IAssetInstrument).Select(x => x as IAssetInstrument);
            return assetTrades.Max(x => x.LastSensitivityDate);
        }
    }
}
