using System;
using System.Numerics;

namespace Qwack.Paths.Features.Rates
{
    public interface IRateFeature
    {
        string RateName { get; set; }
        Span<Vector<double>> GetSpanForPath(int pathId);
    }
}
