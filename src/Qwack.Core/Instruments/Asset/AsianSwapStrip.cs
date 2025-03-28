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
    public class AsianSwapStrip : IAssetInstrument, ISaCcrEnabledCommodity
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public AsianSwap[] Swaplets { get; set; }

        public string[] IrCurves(IAssetFxModel model) => Swaplets.SelectMany(x => x.IrCurves(model)).Distinct().ToArray();
        public string[] AssetIds => Swaplets.Select(x => x.AssetId).ToArray();
        public Currency Currency => Swaplets.First().PaymentCurrency;

        public DateTime LastSensitivityDate => Swaplets.Max(x => x.LastSensitivityDate);

        public Currency PaymentCurrency => Currency;

        public IAssetInstrument Clone() => new AsianSwapStrip
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            Swaplets = Swaplets.Select(x => (AsianSwap)x.Clone()).ToArray()
        };

        public IAssetInstrument SetStrike(double strike) => new AsianSwapStrip
        {
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            Swaplets = Swaplets.Select(x => (AsianSwap)x.SetStrike(strike)).ToArray()
        };

        public FxConversionType FxType(IAssetFxModel model) => Swaplets.First().FxType(model);
        public string FxPair(IAssetFxModel model) => Swaplets.First().FxPair(model);

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) =>
            Swaplets.SelectMany(x => x.PastFixingDates(valDate))
            .ToDictionary(x => x.Key, x => x.Value);

        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) =>
            Swaplets.SelectMany(x => x.PastFixingDatesFx(model,valDate))
            .ToDictionary(x => x.Key, x => x.Value);


        public override bool Equals(object obj) => obj is AsianSwapStrip swapStrip &&
            TradeId == swapStrip.TradeId &&
            Enumerable.SequenceEqual(Swaplets, swapStrip.Swaplets);

        public string HedgingSet { get; set; }

        public SaCcrAssetClass AssetClass { get; set; }
        public string CommodityType { get; set; }

        public double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => Swaplets.Sum(x => x.EffectiveNotional(model, MPOR));
        public double AdjustedNotional(IAssetFxModel model) => Swaplets.Sum(x => x.AdjustedNotional(model));
        public double SupervisoryDelta(IAssetFxModel model) => Swaplets.Average(x => x.SupervisoryDelta(model));
        public double MaturityFactor(DateTime today, double? MPOR = null) => Swaplets.Max(x => x.MaturityFactor(today, MPOR));
        public double TradeNotional(IAssetFxModel model) => Swaplets.Sum(x => x.TradeNotional(model));

        public TO_Instrument ToTransportObject() =>
            new()
            {
                AssetInstrumentType = AssetInstrumentType.AsianSwapStrip,
                AsianSwapStrip = new TO_AsianSwapStrip
                {
                    TradeId = TradeId,
                    Counterparty = Counterparty,
                    PortfolioName = PortfolioName,
                    Swaplets = Swaplets.Select(x => x.ToTransportObject().AsianSwap).ToArray(),
                    HedgingSet = HedgingSet,
                }
            };


    }
}
