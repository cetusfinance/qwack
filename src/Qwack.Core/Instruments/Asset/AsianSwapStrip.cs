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
    }
}
