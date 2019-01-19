using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments
{
    public interface ISaCcrEnabled
    {
        double EffectiveNotional(IAssetFxModel model);
        double AdjustedNotional (IAssetFxModel model);
        double SupervisoryDelta(IAssetFxModel model);
        double MaturityFactor(DateTime today);
        string HedgingSet { get; set; }
    }
}
