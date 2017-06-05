using Qwack.Dates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Underlyings
{
    public interface IUnderlying
    {
        double GetForward(DateTime expiry);
        Frequency SpotLag { get; }
        double Spot { get; }
    }
}
