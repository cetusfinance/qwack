using System.Linq;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class UnpricedAverage : AsianBasisSwap
    {
        public UnpricedAverage() { }

        public new IAssetInstrument Clone() => new UnpricedAverage
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };

        public new IAssetInstrument SetStrike(double strike) => new UnpricedAverage
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplets = PaySwaplets.Select(x => (AsianSwap)x.SetStrike(-strike)).ToArray(),
            RecSwaplets = RecSwaplets.Select(x => (AsianSwap)x.Clone()).ToArray(),
        };


        public override bool Equals(object obj) => obj is UnpricedAverage basisSwap &&
            TradeId == basisSwap.TradeId &&
            Enumerable.SequenceEqual(PaySwaplets, basisSwap.PaySwaplets) &&
            Enumerable.SequenceEqual(RecSwaplets, basisSwap.RecSwaplets);


        public new TO_Instrument ToTransportObject() =>
           new()
           {
               AssetInstrumentType = AssetInstrumentType.UnpricedAverage,
               UnpricedAverage = new TO_UnpricedAverage
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
