using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwapStrip : IAssetInstrument
    {
        public string TradeId { get; set; }

        public AsianSwap[] Swaplets { get; set; }

        public string[] AssetIds => Swaplets.Select(x => x.AssetId).ToArray();

        public DateTime LastSensitivityDate => Swaplets.Max(x => x.LastSensitivityDate);

        public IAssetInstrument Clone() => new AsianSwapStrip
        {
            TradeId = TradeId,
            Swaplets = Swaplets.Select(x => (AsianSwap)x.Clone()).ToArray()
        };

        public IAssetInstrument SetStrike(double strike) => new AsianSwapStrip
        {
            TradeId = TradeId,
            Swaplets = Swaplets.Select(x => (AsianSwap)x.SetStrike(strike)).ToArray()
        };
    }
}
