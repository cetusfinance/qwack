using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianBasisSwap : IAssetInstrument
    {
        public string TradeId { get; set; }

        public AsianSwap[] PaySwaplets { get; set; }
        public AsianSwap[] RecSwaplets { get; set; }

        public string[] AssetIds => PaySwaplets.Select(x => x.AssetId).Concat(RecSwaplets.Select(x => x.AssetId)).Distinct().ToArray();
    }
}
