using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments
{
    public interface IAssetInstrument : IInstrument
    {
        string[] AssetIds { get; }
        string[] IrCurves(IAssetFxModel model);

        Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate);
        Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate);
        FxConversionType FxType(IAssetFxModel model);
        string FxPair(IAssetFxModel model);

        IAssetInstrument Clone();
        IAssetInstrument SetStrike(double strike);

        Currency PaymentCurrency { get; }
    }
}
