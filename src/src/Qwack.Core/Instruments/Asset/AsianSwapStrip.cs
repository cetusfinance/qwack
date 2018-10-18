using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwapStrip : IAssetInstrument
    {
        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public AsianSwap[] Swaplets { get; set; }

        public string[] IrCurves => Swaplets.SelectMany(x => x.IrCurves).Distinct().ToArray();
        public string[] AssetIds => Swaplets.Select(x => x.AssetId).ToArray();
        public Currency Currency => Swaplets.First().PaymentCurrency;

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

        public FxConversionType FxType(IAssetFxModel model) => Swaplets.First().FxType(model);
        public string FxPair(IAssetFxModel model) => Swaplets.First().FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            Swaplets.SelectMany(x => x.PastFixingDates(valDate))
            .ToDictionary(x => x.Key, x => x.Value);

        public override bool Equals(object obj) => obj is AsianSwapStrip swapStrip &&
            TradeId == swapStrip.TradeId &&
            Enumerable.SequenceEqual(Swaplets, swapStrip.Swaplets);
    }
}
