using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Basic
{
    public enum RiskMetric
    {
        FxDelta,
        FxDeltaGamma,
        FxVega,
        AssetCurveDelta,
        AssetCurveDeltaGamma,
        AssetVega,
        Theta,
        PV01,
        BenchmarkIrDelta,
    }
}
