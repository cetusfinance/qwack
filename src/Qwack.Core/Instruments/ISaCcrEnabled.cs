using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments
{
    public interface ISaccrEnabled
    {
        double EffectiveNotional(IAssetFxModel model, double? MPOR=null);
        double AdjustedNotional (IAssetFxModel model);
        double SupervisoryDelta(IAssetFxModel model);
        double MaturityFactor(DateTime today, double? MPOR=null);
        string HedgingSet { get; set; }
    }

    public interface ISaCcrEnabledIR : ISaccrEnabled
    {
        int MaturityBucket(DateTime today);
        double SupervisoryDuration(DateTime today);
        double TradeNotional { get; }
    }

    public interface ISaCcrEnabledCommodity : ISaccrEnabled
    {
        double TradeNotional(IAssetFxModel model);
        SaCcrAssetClass AssetClass { get; set; }
        string CommodityType { get; set; }
    }

    public interface ISaccrEnabledCredit : ISaccrEnabled
    {
        string ReferenceName { get; }
        string ReferenceRating { get; }
        double SupervisoryDuration(DateTime today);
        double TradeNotional { get; }
    }

    public interface ISaccrEnabledFx : ISaccrEnabled
    {
        string Pair { get; }
        double ForeignNotional { get; }
    }

    public interface ISaccrEnabledEquity : ISaccrEnabled
    {
        bool IsIndex { get; }
        double TradeNotional { get; }
        string ReferenceName { get; }
    }
}
