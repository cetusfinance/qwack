using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    [ProtoContract]
    public class TO_ConstantVolSurface : TO_VolSurface_Base
    {
        [ProtoMember(5)]
        public double Volatility { get; set; }   
    }
}
