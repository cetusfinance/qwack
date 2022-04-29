using System;
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
            if (transportObject.SparsePointSurface != null)
                return new SparsePointSurface(transportObject.SparsePointSurface, currencyProvider);

            throw new Exception("Unknown volSurface type");
        }

        public static TO_VolSurface GetTransportObject(this IVolSurface volSurface) => volSurface switch
        {
            RiskyFlySurface rf => new TO_VolSurface { RiskyFlySurface = rf.GetTransportObject() },
            GridVolSurface gs => new TO_VolSurface { GridVolSurface = gs.GetTransportObject() },
            ConstantVolSurface cs => new TO_VolSurface { ConstantVolSurface = cs.GetTransportObject() },
            SparsePointSurface sp => new TO_VolSurface { SparsePointSurface = sp.GetTransportObject() },
            _ => throw new Exception("Unable to serialize volsurface"),
        };
    }
}
