using System.Collections.Generic;
using Qwack.Core.Instruments;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class AssetCurveBenchmarkSpec
    {
        public string CurveName { get; set; }
        public string[] DependsOnCurves { get; set; }
        public List<IAssetInstrument> Instruments { get; set; }
        public PriceCurveType CurveType { get; set; }
    }
}
