using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public interface ITrsUnderlying
    {
        public string[] AssetIds { get; }

        public TO_ITrsUnderlying ToTransportObject();
    }
}
