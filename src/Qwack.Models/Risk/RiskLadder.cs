using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Risk.Mutators;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class RiskLadder : IDisposable
    {
        public Action<double> ProgressAction { get; set; } 

        public ICurrencyProvider CurrencyProvider { get; set; }
        public ICalendarProvider CalendarProvider { get; set; }

        public string AssetId { get; private set; }
        public MutationType ShiftType { get; private set; }
        public RiskMetric Metric { get; private set; }
        public double ShiftSize { get; private set; }
        public int NScenarios { get; private set; }
        public bool ReturnDifferential { get; private set; }
        public Currency Ccy { get; private set; }
        public FxPair FxPair { get; private set; }
        public bool LMESparseDeltaMode { get; set; }
        public bool ParallelizeRiskMetric { get; set; } = true;
        public bool ParallelizeOuterCalc { get; set; } = true;
        public double[] SpecificShifts { get; set; }
        public bool SpecificShiftsAreRelative { get; set; }
        public double FMELambda { get; set; } = 0.99;
        public bool FMEUseImpliedVol { get; set; }

        public RiskLadder(string assetId, MutationType shiftType, RiskMetric metric, double shiftStepSize, int nScenarios, bool returnDifferential = true)
        {
            AssetId = assetId;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSize = shiftStepSize;
            NScenarios = nScenarios;
            ReturnDifferential = returnDifferential;
        }

        public RiskLadder(Currency ccy, MutationType shiftType, RiskMetric metric, double shiftStepSize, int nScenarios, bool returnDifferential = true)
        {
            Ccy = ccy;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSize = shiftStepSize;
            NScenarios = nScenarios;
            ReturnDifferential = returnDifferential;
        }

        public RiskLadder(FxPair pair, MutationType shiftType, RiskMetric metric, double shiftStepSize, int nScenarios, bool returnDifferential = true)
        {
            FxPair = pair;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSize = shiftStepSize;
            NScenarios = nScenarios;
            ReturnDifferential = returnDifferential;
        }

        public Dictionary<string, IPvModel> GenerateScenarios(IPvModel model)
        {
            if(SpecificShifts!=null && SpecificShifts.Length>0)
                return GenerateScenariosSpecific(model);

            var o = new Dictionary<string, IPvModel>();

            var results = new KeyValuePair<string, IPvModel>[NScenarios * 2 + 1];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShift = i * ShiftSize;
                var thisLabel = (string.IsNullOrWhiteSpace(AssetId) ? (FxPair?.ToString() ?? Ccy.Ccy ) : AssetId) + "~" + thisShift;
                if (thisShift == 0)
                    results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, model.Rebuild(model.VanillaModel,model.Portfolio));
                else
                {
                    if (string.IsNullOrWhiteSpace(AssetId) && FxPair==null)
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => FlatShiftMutator.FxSpotShift(Ccy, thisShift, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                    else if (FxPair != null)
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => FlatShiftMutator.FxSpotShift(FxPair, thisShift, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                    else
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => FlatShiftMutator.AssetCurveShift(AssetId, thisShift, model),
                            MutationType.FME => FMEShiftMutator.AssetCurveShift(AssetId, thisShift, FMELambda, FMEUseImpliedVol, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                }
            }).Wait();

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }

        public Dictionary<string, IPvModel> GenerateScenariosSpecific(IPvModel model)
        {
            var o = new Dictionary<string, IPvModel>();
            var n = SpecificShifts.Length;
            var results = new KeyValuePair<string, IPvModel>[n * 2 + 1];
            ParallelUtils.Instance.For(-n, n + 1, 1, (i) =>
            //for(var i=-n;i<=n;i++)
            {
                var thisShift = i == 0 ? 0 : System.Math.Sign(i) * SpecificShifts[System.Math.Abs(i) - 1];
                var thisLabel = (string.IsNullOrWhiteSpace(AssetId) ? (FxPair?.ToString() ?? Ccy.Ccy) : AssetId) + "~" + thisShift;
                if (thisShift == 0)
                    results[i + n] = new KeyValuePair<string, IPvModel>(thisLabel, model.Rebuild(model.VanillaModel, model.Portfolio));
                else
                {
                    if (string.IsNullOrWhiteSpace(AssetId) && FxPair == null)
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => FlatShiftMutator.FxSpotShift(Ccy, thisShift, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + n] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                    else if (FxPair != null)
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => FlatShiftMutator.FxSpotShift(FxPair, thisShift, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + n] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                    else
                    {
                        var shifted = ShiftType switch
                        {
                            MutationType.FlatShift => SpecificShiftsAreRelative ? RelativeShiftMutator.AssetCurveShift(AssetId, thisShift, model) : FlatShiftMutator.AssetCurveShift(AssetId, thisShift, model),
                            MutationType.FME => FMEShiftMutator.AssetCurveShift(AssetId, thisShift, FMELambda, FMEUseImpliedVol, model),
                            _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                        };
                        results[i + n] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                }
            }).Wait();
            //}

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }


        private string[] GetCubeMatchingFields(RiskMetric riskMetric) => riskMetric switch
        {
            RiskMetric.AssetCurveDelta or RiskMetric.AssetVega or RiskMetric.PV01 => ["TradeId", "PointLabel", "AssetId"],
            RiskMetric.PV or RiskMetric.FV or _ => ["TradeId"],
        };

        public ICube Generate(IPvModel model, Portfolio portfolio = null)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type> { { "Scenario", typeof(string) } });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;

            var matchingFields = GetCubeMatchingFields(Metric);
            if (ReturnDifferential)
            {
                var baseModel = model;
                if (portfolio != null)
                {
                    baseModel = baseModel.Rebuild(baseModel.VanillaModel, portfolio);
                }
                baseRiskCube = GetRisk(baseModel);
                baseModel.Dispose();
            }

            var threadLock = new object();
            var results = new ICube[scenarios.Count];
            var scList = scenarios.ToList();

            var taskCount = (double) scList.Count;
            var tasksCompleted = 0;
            ParallelUtils.Instance.For(0, scList.Count, 1, i =>
            {
                var scenario = scList[i];
                var pvModel = scenario.Value;
                if (portfolio != null)
                {
                    pvModel = pvModel.Rebuild(pvModel.VanillaModel, portfolio);
                }
                var result = GetRisk(pvModel);
                
                pvModel.Dispose();
                
                if (ReturnDifferential)
                {
                    try
                    {
                        result = result.Difference(baseRiskCube, matchingFields);
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }

                results[i] = result;
                
                Interlocked.Increment(ref tasksCompleted);
                ProgressAction?.Invoke(Convert.ToDouble(tasksCompleted) / taskCount);

            }, !ParallelizeOuterCalc).Wait();

            for (var i = 0; i < results.Length; i++)
            {
                o = (ResultCube)o.Merge(results[i],
                    new Dictionary<string, object> { { "Scenario", scList[i].Key } }, null, true);
            }

            return o;
        }

        public ICube GenerateFromCubes(Dictionary<string,ICube> cubes, ICube baseRiskCube)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type> { { "Scenario", typeof(string) } });

            var matchingFields = GetCubeMatchingFields(Metric);

            var threadLock = new object();
            var results = new ICube[cubes.Count];
            var scList = cubes.ToList();

            var taskCount = (double)scList.Count;
            var tasksCompleted = 0;
            for (var i = 0; i < scList.Count; i++)
            {
                var scenario = scList[i];
                var result = scenario.Value;

                if (ReturnDifferential)
                {
                    try
                    {
                        result = result.Difference(baseRiskCube, matchingFields);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                results[i] = result;

                Interlocked.Increment(ref tasksCompleted);
                ProgressAction?.Invoke(Convert.ToDouble(tasksCompleted) / taskCount);
            }

            for (var i = 0; i < results.Length; i++)
            {
                o = (ResultCube)o.Merge(results[i],
                    new Dictionary<string, object> { { "Scenario", scList[i].Key } }, null, true);
            }

            return o;
        }



        private ICube GetRisk(IPvModel model) => Metric switch
        {
            RiskMetric.AssetCurveDelta => model.AssetDeltaSingleCurve(AssetId, isSparseLMEMode: LMESparseDeltaMode, calendars: CalendarProvider, parallelize:ParallelizeRiskMetric),
            RiskMetric.AssetVega => model.AssetVega(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency, ParallelizeRiskMetric),
            RiskMetric.PV => model.PV(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency),
            RiskMetric.FV => model.FV(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency),
            RiskMetric.PV01 => model.AssetIrDelta(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency, paralellize: ParallelizeRiskMetric),
            RiskMetric.FxDelta => model.FxDelta(FxPair?.Foreign ?? Ccy ?? model.VanillaModel.FundingModel.FxMatrix.BaseCurrency, CurrencyProvider, false, ShouldInvert(FxPair?.Foreign ?? Ccy ?? model.VanillaModel.FundingModel.FxMatrix.BaseCurrency)),
            _ => throw new Exception($"Unable to process risk metric {Metric}"),
        };

        static string[] _inverseCcys = new[] { "EUR", "GBP", "AUD", "NZD" };
        private static bool ShouldInvert(string ccy) => !_inverseCcys.Contains(ccy);

        public void Dispose()
        {

        }
    }
}
