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
        public static TO_Instrument GetTransportObject(this IInstrument instrument)
        {
            switch(instrument)
            {
                case AsianOption asianOption:
                    return asianOption.GetTransportObject();
                case AsianSwap asianSwap:
                    return asianSwap.GetTransportObject();
                case AsianSwapStrip asianSwapStrip:
                    return asianSwapStrip.GetTransportObject();
                case EuropeanOption europeanOption:
                    return europeanOption.GetTransportObject();
                case Forward forward:
                    return forward.GetTransportObject();
                default:
                    throw new Exception("Unable to serialize instrument");
            }
        }
    }
}
