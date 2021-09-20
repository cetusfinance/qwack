using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Core.Instruments.Asset;
using Qwack.Transport.TransportObjects.Instruments;

namespace Qwack.Core.Instruments
{
    public static class InstrumentEx
    {
        public static TO_Instrument GetTransportObject(this IInstrument instrument) => instrument switch
        {
            AsianOption asianOption => asianOption.ToTransportObject(),
            AsianSwap asianSwap => asianSwap.ToTransportObject(),
            AsianSwapStrip asianSwapStrip => asianSwapStrip.ToTransportObject(),
            EuropeanOption europeanOption => europeanOption.ToTransportObject(),
            Forward forward => forward.ToTransportObject(),
            _ => throw new Exception("Unable to serialize instrument"),
        };
    }
}
