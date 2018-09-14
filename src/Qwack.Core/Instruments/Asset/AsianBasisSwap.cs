using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianBasisSwap : IAssetInstrument
    {
        public string TradeId { get; set; }

        public AsianSwap[] PaySwaplets { get; set; }
        public AsianSwap[] RecSwaplets { get; set; }

        public string[] AssetIds => PaySwaplets.Select(x => x.AssetId).Concat(RecSwaplets.Select(x => x.AssetId)).Distinct().ToArray();

        public IAssetInstrument Clone() => new AsianBasisSwap
        {
            TradeId = TradeId,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };

        public IAssetInstrument SetStrike(double strike) => new AsianBasisSwap
        {
            TradeId = TradeId,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.SetStrike(strike)).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };
    }
}
