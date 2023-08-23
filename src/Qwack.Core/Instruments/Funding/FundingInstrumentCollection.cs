using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    /// <summary>
    /// Just a specific class for now, later we will need more features in this 
    /// so this gives us an easy way to do that without having to change interfaces
    /// </summary>
    public class FundingInstrumentCollection : List<IFundingInstrument>
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        public List<string> SolveCurves => this.Select(x => x.SolveCurve).Distinct().ToList();

        public FundingInstrumentCollection(ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
        }

        public FundingInstrumentCollection Clone()
        {
            var fic = new FundingInstrumentCollection(_currencyProvider, _calendarProvider);
            fic.AddRange(this.Select(x => x.Clone()));
            return fic;
        }

        public Dictionary<string, IIrCurve> ImplyContainedCurves(DateTime buildDate, Interpolator1DType interpType, 
            CpiInterpolationType cpiInterpolationType = CpiInterpolationType.IndexLevel, Dictionary<string, double> spotCpiLevels = null, 
            Dictionary<string, double[]> cpiSeasonalalFactors = null, Dictionary<string, Dictionary<DateTime, double>> fixings = null)
        {
            var o = new Dictionary<string, IIrCurve>();
            spotCpiLevels ??= new();
            fixings ??= new(); 
            cpiSeasonalalFactors ??= new();

            foreach (var curveName in SolveCurves)
            {
                var insInScope = this.Where(x => x.SolveCurve == curveName).ToList();
                var pillars = insInScope.Select(x => x.PillarDate)
                    .OrderBy(x => x)
                    .ToArray();
                if (pillars.Distinct().Count() != pillars.Length)
                    throw new Exception($"More than one instrument has the same solve pillar on curve {curveName}");

                var ccy = _currencyProvider.GetCurrency(curveName.Split('.')[0]);
                var colSpec = (curveName.Contains("[")) ? curveName.Split('[').Last().Trim("[]".ToCharArray()) : curveName.Substring(curveName.IndexOf('.') + 1);
                if (o.Values.Any(v => v.CollateralSpec == colSpec))
                    colSpec = colSpec + "_" + curveName;

                if (insInScope.All(i => i is IIsInflationInstrument))
                {
                    var dummyRates = pillars.Select(x => 100.0).ToArray();
                    double? cpiSpot = spotCpiLevels.TryGetValue(curveName, out var cs) ? cs : null;
                    var cpiSeasonalAdj = cpiSeasonalalFactors.TryGetValue(curveName, out var sf) ? sf : null;
                    var fixingForIx = fixings.TryGetValue(curveName, out var f) ? f : null;
                    DateTime? spotDate = null;
                    if (fixingForIx != null)
                    {
                        spotDate = fixingForIx.Keys.Max();
                        cpiSpot = fixingForIx[spotDate.Value];
                    }
                    if (cpiSeasonalAdj == null)
                    {
                        var irCurve = new CPICurve(buildDate, pillars, dummyRates, DayCountBasis.Act360, new Frequency("-3m"), _calendarProvider.GetCalendarSafe(ccy), cpiInterpolationType, spotDate, cpiSpot, fixingForIx)
                        {
                            Name = curveName,
                            CollateralSpec = colSpec,
                        };
                        o.Add(curveName, irCurve);
                    }
                    else
                    {
                        var irCurve = new SeasonalCpiCurve(buildDate, pillars, dummyRates, DayCountBasis.Act360,  new Frequency("-3m"), _calendarProvider.GetCalendarSafe(ccy), cpiSeasonalAdj, cpiInterpolationType, spotDate, cpiSpot, fixingForIx)
                        {
                            Name = curveName,
                            CollateralSpec = colSpec,
                        };
                        o.Add(curveName, irCurve);
                    }
                }
                else
                {
                    var dummyRates = pillars.Select(x => 0.05).ToArray();
                    var irCurve = new IrCurve(pillars, dummyRates, buildDate, curveName, interpType, ccy, colSpec);
                    o.Add(curveName, irCurve);
                }
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
                var deps = insForCurve.SelectMany(x => x.Dependencies(matrix)).Where(x => x != curveName).Distinct();
                if (deps.Any())
                    dependencies[curveName].AddRange(deps);
            }

            var currentStage = 0;
            //first find any curves depending on no other
            var noDepCurves = dependencies.Where(x => !x.Value.Any()).ToArray();
            foreach (var curve in noDepCurves)
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
                foreach (var curve in canResolveThisTime.ToList())
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

                currentCount = dependencies.Count();
            }
            return o;
        }

        public Dictionary<string, SolveStage> ImplySolveStages2(IFxMatrix matrix)
        {
            var o = new Dictionary<string, SolveStage>();

            var dependencies = new Dictionary<string, List<string>>();
            foreach (var curveName in SolveCurves)
            {
                var insForCurve = this.Where(x => x.SolveCurve == curveName);
                dependencies.Add(curveName, new List<string>());
                var deps = insForCurve.SelectMany(x => x.Dependencies(matrix)).Where(x => x != curveName).Distinct();
                if (deps.Any())
                    dependencies[curveName].AddRange(deps);
            }

            var currentStage = 0;
            var currentSub = 0;
            //first find any curves depending on no other
            var noDepCurves = dependencies.Where(x => !x.Value.Any()).ToArray();
            foreach (var curve in noDepCurves)
            {
                o.Add(curve.Key, new SolveStage { Stage = currentStage, SubStage = currentSub });
                currentSub++;
                dependencies.Remove(curve.Key);
            }

            currentStage++;

            var currentCount = dependencies.Count();
            while (dependencies.Any())
            {
                currentSub = 0;
                //first do curves which only have resolved dependencies
                var resolvedCurves = o.Keys.ToList();
                var canResolveThisTime = dependencies.Where(x => x.Value.All(y => resolvedCurves.Contains(y)));
                foreach (var curve in canResolveThisTime.ToList())
                {
                    o.Add(curve.Key, new SolveStage { Stage = currentStage, SubStage = currentSub });
                    currentSub++;
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

                    o.Add(curve, new SolveStage { Stage = currentStage, SubStage = currentSub });
                    dependencies.Remove(curve);
                    var sisterCurve = coCurves[curve].First();
                    o.Add(sisterCurve, new SolveStage { Stage = currentStage, SubStage = currentSub });
                    dependencies.Remove(sisterCurve);
                    currentSub++;
                }

                if (currentCount == dependencies.Count())
                    throw new Exception($"Failed to make forward progress at stage {currentStage}");

                currentCount = dependencies.Count();
                currentStage++;
            }
            return o;
        }


        public Dictionary<string, List<string>> FindDependencies(IFxMatrix matrix)
        {

            var dependencies = new Dictionary<string, List<string>>();
            foreach (var curveName in SolveCurves)
            {
                var insForCurve = this.Where(x => x.SolveCurve == curveName);
                dependencies.Add(curveName, new List<string>());
                var deps = insForCurve.SelectMany(x => x.Dependencies(matrix)).Distinct();
                if (deps.Any())
                    dependencies[curveName].AddRange(deps);
            }

            var sumAllDeps = -1;
            var breakout = 0;
            while (dependencies.Sum(x => x.Value.Count) > sumAllDeps && breakout < 20)
            {
                sumAllDeps = dependencies.Sum(x => x.Value.Count);

                foreach (var curveName in SolveCurves)
                {
                    var depsForCurve = dependencies[curveName];
                    var newTotalDeps = depsForCurve.SelectMany(d => dependencies[d]).Distinct().ToList();
                    dependencies[curveName] = newTotalDeps;
                }

                breakout++;
            }

            return dependencies;

        }

        public Dictionary<string, List<string>> FindDependenciesInverse(IFxMatrix matrix)
        {
            var deps = FindDependencies(matrix);

            var invDeps = new Dictionary<string, List<string>>();

            foreach (var d in deps.Keys.ToArray())
            {
                invDeps[d] = deps.Where(x => x.Value.Contains(d)).Select(x => x.Key).ToList();
            }

            return invDeps;
        }
    }

    public class SolveStage
    {
        public int Stage { get; set; }
        public int SubStage { get; set; }
    }
}
