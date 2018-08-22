using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Curves;
using Qwack.Math.Interpolation;

namespace Qwack.Core.Instruments.Funding
{
    /// <summary>
    /// Just a specific class for now, later we will need more features in this 
    /// so this gives us an easy way to do that without having to change interfaces
    /// </summary>
    public class FundingInstrumentCollection:List<IFundingInstrument>
    {
        public List<string> SolveCurves => this.Select(x => x.SolveCurve).Distinct().ToList();

        public Dictionary<string, IrCurve> ImplyContainedCurves(DateTime buildDate, Interpolator1DType interpType)
        {
            var o = new Dictionary<string, IrCurve>();

            foreach(var curveName in SolveCurves)
            {
                var pillars = this.Where(x => x.SolveCurve == curveName)
                    .Select(x => x.PillarDate)
                    .OrderBy(x => x)
                    .ToArray();
                if (pillars.Distinct().Count() != pillars.Count())
                    throw new Exception($"More than one instrument has the same solve pillar on curve {curveName}");

                var dummyRates = pillars.Select(x => 0.05).ToArray();
                var irCurve = new IrCurve(pillars, dummyRates, buildDate, curveName, interpType);
                o.Add(curveName, irCurve);
            }
            return o;
        }
    }
}
