using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Curves
{
    public enum PriceCurveType
    {
        Linear=0,
        LME=0,
        Next=1,
        NYMEX=1,
        NextButOnExpiry=2,
        ICE=2
    }
}
