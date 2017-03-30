using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Options.VolSurfaces
{
    public interface IVolSurface
    {
        double GetVolForAbsoluteStrike(double strike, DateTime expiry);
        double GetVolForAbsoluteStrike(double strike, double maturity);
        double GetVolForDeltaStrike(double strike, DateTime expiry);
        double GetVolForDeltaStrike(double deltaStrike, double maturity);
        DateTime OriginDate { get; }
        double GetFwdATMVol(DateTime startDate, DateTime endDate);
    }
}
