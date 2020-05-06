using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Options.VolSurfaces
{
    public static class VolSurfaceFactory
    {
        public static IVolSurface GetVolSurface(this TO_VolSurface transportObject, ICurrencyProvider currencyProvider)
        {
            if (transportObject.ConstantVolSurface != null)
                return new ConstantVolSurface(transportObject.ConstantVolSurface, currencyProvider);
            if (transportObject.RiskyFlySurface != null)
                return new RiskyFlySurface(transportObject.RiskyFlySurface, currencyProvider);
            if (transportObject.GridVolSurface != null)
                return new GridVolSurface(transportObject.GridVolSurface, currencyProvider);

            throw new Exception("Unknown volSurface type");
        }

        public static TO_VolSurface GetTransportObject(this IVolSurface volSurface)
        {
            switch(volSurface)
            {
                case RiskyFlySurface rf:
                    return new TO_VolSurface { RiskyFlySurface = rf.GetTransportObject() };
                case GridVolSurface gs:
                    return new TO_VolSurface { GridVolSurface = gs.GetTransportObject() };
                case ConstantVolSurface cs:
                    return new TO_VolSurface { ConstantVolSurface = cs.GetTransportObject() };
                default:
                    throw new Exception("Unable to serialize volsurface");
            }
        }
    }
}
