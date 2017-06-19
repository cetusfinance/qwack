using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Options.VolSurfaces
{
    public interface IATMVolSurface : IVolSurface
    {
        double GetForwardATMVol(DateTime startDate, DateTime endDate);
        double GetForwardATMVol(double start, double end);
    }
}
