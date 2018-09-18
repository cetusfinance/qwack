using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments
{
    public interface IAssetInstrument : IInstrument
    {
        string[] AssetIds { get; }
        DateTime LastSensitivityDate { get; }
        IAssetInstrument Clone();
        IAssetInstrument SetStrike(double strike);
    }
}
