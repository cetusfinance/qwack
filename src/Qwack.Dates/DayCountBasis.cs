using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public enum DayCountBasis
    {
        ACT360 = 360,
        Act_360 = 360,
        ACT365 = 365,
        Act_365 = 365,
        Act_Act = 365,

        Act_Act_ISDA,
        Act_Act_ICMA,

        Act_365F,
        Act_364,
        _30_360 = 30360,
        Thirty360 = 30360,

        Unity
    }
}
