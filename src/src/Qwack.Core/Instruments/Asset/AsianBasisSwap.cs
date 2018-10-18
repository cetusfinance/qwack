using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianBasisSwap : IAssetInstrument
    {
        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public AsianSwap[] PaySwaplets { get; set; }
        public AsianSwap[] RecSwaplets { get; set; }


        public string[] AssetIds => PaySwaplets.Select(x => x.AssetId).Concat(RecSwaplets.Select(x => x.AssetId)).Distinct().ToArray();
        public string[] IrCurves => PaySwaplets.SelectMany(x => x.IrCurves).Concat(RecSwaplets.SelectMany(x => x.IrCurves)).Distinct().ToArray();
        public Currency Currency => PaySwaplets.First().PaymentCurrency;

        public DateTime LastSensitivityDate => PaySwaplets.Max(x => x.LastSensitivityDate).Max(PaySwaplets.Max(x => x.LastSensitivityDate));

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


        public FxConversionType FxType(IAssetFxModel model) => PaySwaplets.First().FxType(model);
        public string FxPair(IAssetFxModel model) => PaySwaplets.First().FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            PaySwaplets.SelectMany(x => x.PastFixingDates(valDate))
            .Concat(RecSwaplets.SelectMany(x => x.PastFixingDates(valDate)))
            .Distinct()
            .ToDictionary(x => x.Key, x => x.Value);

        public override bool Equals(object obj) => obj is AsianBasisSwap basisSwap &&
            TradeId == basisSwap.TradeId &&
            Enumerable.SequenceEqual(PaySwaplets, basisSwap.PaySwaplets) &&
            Enumerable.SequenceEqual(RecSwaplets, basisSwap.RecSwaplets);
    }
}
