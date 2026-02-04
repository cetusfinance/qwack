using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianBasisSwap : IAssetInstrument
    {
        public AsianBasisSwap() { }
        
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public string HedgingSet { get; set; }
        public AsianSwap[] PaySwaplets { get; set; }
        public AsianSwap[] RecSwaplets { get; set; }

        public string[] AssetIds => PaySwaplets.Select(x => x.AssetId).Concat(RecSwaplets.Select(x => x.AssetId)).Distinct().ToArray();
        public string[] IrCurves(IAssetFxModel model) => PaySwaplets.SelectMany(x => x.IrCurves(model)).Concat(RecSwaplets.SelectMany(x => x.IrCurves(model))).Distinct().ToArray();
        public Currency Currency => PaySwaplets.First().PaymentCurrency;
        public Currency PaymentCurrency => Currency;
        public DateTime LastSensitivityDate => PaySwaplets.Max(x => x.LastSensitivityDate).Max(PaySwaplets.Max(x => x.LastSensitivityDate));

        public IAssetInstrument Clone() => new AsianBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };

        public IAssetInstrument SetStrike(double strike) => new AsianBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.SetStrike(-strike)).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };

        public double Strike => -PaySwaplets.First().Strike;

        public FxConversionType FxType(IAssetFxModel model) => PaySwaplets.First().FxType(model);
        public string FxPair(IAssetFxModel model) => PaySwaplets.First().FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            PaySwaplets.SelectMany(x => x.PastFixingDates(valDate))
            .Concat(RecSwaplets.SelectMany(x => x.PastFixingDates(valDate)))
            .GroupBy(x=>x.Key)
            .ToDictionary(x => x.Key, x => x.SelectMany(x=>x.Value).Distinct().ToList());

        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) =>
            PaySwaplets.SelectMany(x => x.PastFixingDatesFx(model, valDate))
            .Concat(RecSwaplets.SelectMany(x => x.PastFixingDatesFx(model, valDate)))
            .GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.SelectMany(x => x.Value).Distinct().ToList());

        public override bool Equals(object obj) => obj is AsianBasisSwap basisSwap &&
            TradeId == basisSwap.TradeId &&
            Enumerable.SequenceEqual(PaySwaplets, basisSwap.PaySwaplets) &&
            Enumerable.SequenceEqual(RecSwaplets, basisSwap.RecSwaplets);

        public override int GetHashCode() => TradeId.GetHashCode() ^ PaySwaplets.GetHashCode() ^ RecSwaplets.GetHashCode();

        public TO_Instrument ToTransportObject() =>
           new()
           {
               AssetInstrumentType = AssetInstrumentType.AsianBasisSwap,
               AsianBasisSwap = new TO_AsianBasisSwap
               {
                   TradeId = TradeId,
                   Counterparty = Counterparty,
                   PortfolioName = PortfolioName,
                   PaySwaplets = PaySwaplets.Select(x => x.ToTransportObject().AsianSwap).ToArray(),
                   RecSwaplets = RecSwaplets.Select(x => x.ToTransportObject().AsianSwap).ToArray(),
                   HedgingSet = HedgingSet,
               }
           };

    }
}
