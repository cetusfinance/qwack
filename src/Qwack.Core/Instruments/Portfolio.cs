using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments
{
    public class Portfolio : IInstrument
    {

        public List<IInstrument> Instruments { get; set; }

        public string TradeId => throw new NotImplementedException();
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

        public static string[] AssetIds(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            if(!assetTrades.Any())
                return new string[0];

            return assetTrades.SelectMany(x => ((IAssetInstrument)x).AssetIds).Distinct().ToArray();
        }

        public static string[] FxPairs(this Portfolio portfolio, IAssetFxModel model)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);
            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward);

            if (!fxTrades.Any() && !assetTrades.Any())
                return new string[0];

            var o = new List<string>();

            if(fxTrades.Any())
            {
                o.AddRange(fxTrades.Select(x => ((FxForward)x).Pair));
            }
            if(assetTrades.Any())
            {
                var compoTrades = assetTrades.Select(x=>x as IAssetInstrument)
                    .Where(x => x.FxType(model) != FxConversionType.None);
                if(compoTrades.Any())
                {
                    o.AddRange(compoTrades.Select(x => x.FxPair(model)));
                }
            }

            return o.Distinct().ToArray();
        }
    }
}
