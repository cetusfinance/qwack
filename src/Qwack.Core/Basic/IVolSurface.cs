using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Core.Basic
{
    public interface IVolSurface
    {
        double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward);
        double GetVolForAbsoluteStrike(double strike, double maturity, double forward);
        double GetVolForDeltaStrike(double strike, DateTime expiry, double forward);
        double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward);
        DateTime OriginDate { get; }

    }
}
