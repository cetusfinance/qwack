using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.TransportObjects.MarketData.VolSurfaces
{
    public class TO_VolSurface
    {
        public TO_ConstantVolSurface ConstantVolSurface { get; set; }
        public TO_GridVolSurface GridVolSurface { get; set; }
        public TO_RiskyFlySurface RiskyFlySurface { get; set; }
    }
}

