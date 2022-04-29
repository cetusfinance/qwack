using System.Collections.Generic;
using Qwack.Core.Instruments;

namespace Qwack.Core.Trades
{
    public class TradeSet
    {
        public List<ExecutedTrade> ExecutedTrades { get; set; }
        public Dictionary<int, IAssetInstrument> AssetInstruments { get; set; }
    }
}
