using System;
using Qwack.Core.Basic;

namespace Qwack.Options.VolSurfaces
{
    public interface IATMVolSurface : IVolSurface
    {
        double GetForwardATMVol(DateTime startDate, DateTime endDate);
        double GetForwardATMVol(double start, double end);
    }
}
