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
    public class AssetFxBasisSwap : IAssetInstrument
    {
        public AssetFxBasisSwap() { }
        
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public string HedgingSet { get; set; }
        public AsianSwap PaySwaplet { get; set; }
        public AsianSwap RecSwaplet { get; set; }

        public string[] AssetIds => new[] { PaySwaplet.AssetId };
        public string[] IrCurves(IAssetFxModel model) => new[] { PaySwaplet.DiscountCurve, RecSwaplet.DiscountCurve };
        public Currency Currency => PaySwaplet.PaymentCurrency;
        public Currency PaymentCurrency => Currency;
        public DateTime LastSensitivityDate => PaySwaplet.LastSensitivityDate.Max(RecSwaplet.LastSensitivityDate);

        public IAssetInstrument Clone() => new AssetFxBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplet = (AsianSwap)PaySwaplet.Clone(),
            RecSwaplet = (AsianSwap)RecSwaplet.Clone()
        };

        public IAssetInstrument SetStrike(double strike) => new AssetFxBasisSwap
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            HedgingSet = HedgingSet,
            PaySwaplet = (AsianSwap)PaySwaplet.SetStrike(-strike),
            RecSwaplet = (AsianSwap)RecSwaplet.Clone(),
        };

        public double PremiumPay => PaySwaplet.Strike;
        public double PremiumRec => PaySwaplet.Strike;

        public FxConversionType FxType(IAssetFxModel model) => PaySwaplet.FxType(model);
        public string FxPair(IAssetFxModel model) => $"{PaySwaplet.Currency}/{RecSwaplet.Currency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            PaySwaplet.PastFixingDates(valDate)
            .Concat(RecSwaplet.PastFixingDates(valDate))
            .Distinct()
            .ToDictionary(x => x.Key, x => x.Value);

        public override bool Equals(object obj) => obj is AssetFxBasisSwap basisSwap &&
            TradeId == basisSwap.TradeId &&
            PaySwaplet == basisSwap.PaySwaplet &&
            RecSwaplet ==  basisSwap.RecSwaplet;

        public override int GetHashCode() => TradeId.GetHashCode() ^ PaySwaplet.GetHashCode() ^ RecSwaplet.GetHashCode();

        public TO_Instrument ToTransportObject() =>
           new()
           {
               AssetInstrumentType = AssetInstrumentType.AssetFxBasisSwap,
               AssetFxBasisSwap = new TO_AssetFxBasisSwap
               {
                   TradeId = TradeId,
                   Counterparty = Counterparty,
                   PortfolioName = PortfolioName,
                   PaySwaplet = PaySwaplet.ToTransportObject().AsianSwap,
                   RecSwaplet = RecSwaplet.ToTransportObject().AsianSwap,
                   HedgingSet = HedgingSet,
               }
           };

    }
}
