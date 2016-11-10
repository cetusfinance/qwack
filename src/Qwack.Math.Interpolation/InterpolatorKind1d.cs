using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Math.Interpolation
{
    public enum InterpolatorKind1d
    {
        CubicSpline,
        Linear,
        LinearFlatExtrap,
        FloaterHormannRational,
        LogLinear,
        VectorLinearFlatExtrap,
        VectorLinearFlatExtrapFaster
    }
}
