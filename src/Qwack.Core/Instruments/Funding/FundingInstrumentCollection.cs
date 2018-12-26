using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Math.Interpolation;

namespace Qwack.Core.Instruments.Funding
{
    /// <summary>
    /// Just a specific class for now, later we will need more features in this 
    /// so this gives us an easy way to do that without having to change interfaces
    /// </summary>
    public class FundingInstrumentCollection:List<IFundingInstrument>
    {
        private ICurrencyProvider _currencyProvider;

        public List<string> SolveCurves => this.Select(x => x.SolveCurve).Distinct().ToList();

        public FundingInstrumentCollection(ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
        }

        public FundingInstrumentCollection Clone()
        {
            var fic = new FundingInstrumentCollection(_currencyProvider);
            fic.AddRange(this.Select(x => x.Clone()));
            return fic;
        }


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
                var ccy = _currencyProvider.GetCurrency(curveName.Split('.')[0]);
                var colSpec = (curveName.Contains("[")) ? curveName.Split('[').Last().Trim("[]".ToCharArray()) : curveName.Split('.').Last();
                var irCurve = new IrCurve(pillars, dummyRates, buildDate, curveName, interpType, ccy, colSpec);
                o.Add(curveName, irCurve);
            }
            return o;
        }

        public Dictionary<string, int> ImplySolveStages(IFxMatrix matrix)
        {
            var o = new Dictionary<string, int>();

            var dependencies = new Dictionary<string, List<string>>();
            foreach (var curveName in SolveCurves)
            {
                var insForCurve = this.Where(x => x.SolveCurve == curveName);
                dependencies.Add(curveName, new List<string>());
                var deps = insForCurve.SelectMany(x => x.Dependencies(matrix)).Distinct();
                if (deps.Any())
                    dependencies[curveName].AddRange(deps);
            }

            var currentStage = 0;
            //first find any curves depending on no other
            var noDepCurves = dependencies.Where(x => !x.Value.Any());
            foreach(var curve in noDepCurves)
            {
                o.Add(curve.Key, currentStage);
                currentStage++;
                dependencies.Remove(curve.Key);
            }

            var currentCount = dependencies.Count();
            while (dependencies.Any())
            {
                //first do curves which only have resolved dependencies
                var resolvedCurves = o.Keys.ToList();
                var canResolveThisTime = dependencies.Where(x => x.Value.All(y => resolvedCurves.Contains(y)));
                foreach (var curve in canResolveThisTime)
                {
                    o.Add(curve.Key, currentStage);
                    currentStage++;
                    dependencies.Remove(curve.Key);
                }

                //next detect curves which can be co-solved
                var singleDependencyCurves = dependencies.Where(x => x.Value.Count() == 1).ToDictionary(x => x.Key, x => x.Value);
                var coCurves = singleDependencyCurves.Where(x => singleDependencyCurves.ContainsKey(x.Value.First())).ToDictionary(x => x.Key, x => x.Value);
                var coCurveKeys = coCurves.Keys;
                foreach (var curve in coCurveKeys)
                {
                    if (o.ContainsKey(curve))
                        continue;

                    o.Add(curve, currentStage);
                    dependencies.Remove(curve);
                    var sisterCurve = coCurves[curve].First();
                    o.Add(sisterCurve, currentStage);
                    dependencies.Remove(sisterCurve);
                    currentStage++;
                }

                if (currentCount == dependencies.Count())
                    throw new Exception($"Failed to make forward progress at stage {currentStage}");
            }
            return o;
        }
    }
}
