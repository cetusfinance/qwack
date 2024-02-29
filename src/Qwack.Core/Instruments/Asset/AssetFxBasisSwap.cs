using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class AssetFxBasisSwap : IAssetInstrument
    {
        public AssetFxBasisSwap() { }
        
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public string HedgingSet { get; set; }
        public AsianSwap BaseSwaplet { get; set; }
        public string[] AssetIds => new[] { BaseSwaplet.AssetId };
        public string[] IrCurves(IAssetFxModel model) => BaseSwaplet.IrCurves(model);
        public Currency Currency => BaseSwaplet.PaymentCurrency;
        public Currency PaymentCurrency => Currency;
        public DateTime LastSensitivityDate => BaseSwaplet.LastSensitivityDate;

        public IAssetInstrument Clone() => new AssetFxBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            BaseSwaplet = (AsianSwap)BaseSwaplet.Clone(),
        };

        public IAssetInstrument SetStrike(double strike) => new AssetFxBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            BaseSwaplet = (AsianSwap)BaseSwaplet.SetStrike(-strike),
        };

        public double PremiumPay => BaseSwaplet.Strike;
        public double PremiumRec => BaseSwaplet.Strike;

        public FxConversionType FxType(IAssetFxModel model) => BaseSwaplet.FxType(model);
        public string FxPair(IAssetFxModel model) => BaseSwaplet.FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            BaseSwaplet.PastFixingDates(valDate).ToDictionary(x => x.Key, x => x.Value);

        public override bool Equals(object obj) => obj is AssetFxBasisSwap basisSwap &&
            TradeId == basisSwap.TradeId &&
            BaseSwaplet == basisSwap.BaseSwaplet;

        public override int GetHashCode() => TradeId.GetHashCode() ^ BaseSwaplet.GetHashCode();

        public TO_Instrument ToTransportObject() =>
           new()
           {
               AssetInstrumentType = AssetInstrumentType.AssetFxBasisSwap,
               AssetFxBasisSwap = new TO_AssetFxBasisSwap
               {
                   TradeId = TradeId,
                   Counterparty = Counterparty,
                   PortfolioName = PortfolioName,
                   BaseSwaplet = BaseSwaplet.ToTransportObject().AsianSwap,
                   HedgingSet = HedgingSet,
               }
           };

    }
}
