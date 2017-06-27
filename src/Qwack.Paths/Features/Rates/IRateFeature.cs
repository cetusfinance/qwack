using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Qwack.Paths.Features.Rates
{
    public interface IRateFeature
    {
        string RateName { get; set; }
        Span<Vector<double>> GetSpanForPath(int pathId);
    }
}
