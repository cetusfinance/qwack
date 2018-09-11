using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Instruments
{
    public interface IAssetInstrument : IInstrument
    {
        string[] AssetIds { get; }
    }
}
