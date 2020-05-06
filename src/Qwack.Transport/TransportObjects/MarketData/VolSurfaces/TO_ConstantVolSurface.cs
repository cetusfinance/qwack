using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    public class TO_ConstantVolSurface : TO_VolSurface_Base
    {
        public double Volatility { get; set; }   
    }
}
