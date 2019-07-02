using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class RiskMatrix
    {
        private readonly ICurrencyProvider _currencyProvider;

        public string AssetId { get; private set; }
        public Currency Ccy { get; private set; }
        public MutationType ShiftType { get; private set; }
        public RiskMetric Metric { get; private set; }
        public double ShiftSizeAsset { get; private set; }
        public double ShiftSizeFx { get; private set; }
        public int NScenarios { get; private set; }
        public bool ReturnDifferential { get; private set; }

        public FxPair Pair1 { get; private set; }
        public FxPair Pair2 { get; private set; }

        public List<FxPair> FxPairsForDelta { get; set; }

        public RiskMatrix(string assetId, Currency ccy, MutationType shiftType, RiskMetric metric, double shiftStepSizeAsset, double shiftStepSizeFx, int nScenarios, ICurrencyProvider currencyProvider, bool returnDifferential=true)
        {
            AssetId = assetId;
            Ccy = ccy;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSizeAsset = shiftStepSizeAsset;
            ShiftSizeFx = shiftStepSizeFx;
            NScenarios = nScenarios;
            _currencyProvider = currencyProvider;
            ReturnDifferential = returnDifferential;
        }

        public RiskMatrix(FxPair p1, FxPair p2, MutationType shiftType, RiskMetric metric, double shiftStepSize1, double shiftStepSize2, int nScenarios, ICurrencyProvider currencyProvider, bool returnDifferential = true)
        {
            Pair1 = p1;
            Pair2 = p2;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSizeAsset = shiftStepSize1;
            ShiftSizeFx = shiftStepSize2;
            NScenarios = nScenarios;
            _currencyProvider = currencyProvider;
            ReturnDifferential = returnDifferential;
        }

        public RiskMatrix(Currency c1, Currency c2, MutationType shiftType, RiskMetric metric, double shiftStepSize1, double shiftStepSize2, int nScenarios, ICurrencyProvider currencyProvider, bool returnDifferential = true)
        {
        }

        public Dictionary<Tuple<string,string>, IPvModel> GenerateScenarios(IPvModel model)
        {
            if (!string.IsNullOrEmpty(AssetId))
                return GenerateScenariosAssetFx(model);
            else
                return GenerateScenariosFxFx(model);
        }


        private Dictionary<Tuple<string, string>, IPvModel> GenerateScenariosAssetFx(IPvModel model)
        {
            var o = new Dictionary<Tuple<string, string>, IPvModel>();
            var axisLength = NScenarios * 2 + 1;
            var results = new KeyValuePair<Tuple<string, string>, IPvModel>[axisLength * axisLength];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShiftAsset = i * ShiftSizeAsset;
                var thisLabelAsset = AssetId + "~" + thisShiftAsset;

                var assetIx = i + NScenarios;

                IPvModel shifted;

                if (thisShiftAsset == 0)
                    shifted = model;
                else
                    switch (ShiftType)
                    {
                        case MutationType.FlatShift:
                            shifted = FlatShiftMutator.AssetCurveShift(AssetId, thisShiftAsset, model);
                            break;
                        default:
                            throw new Exception($"Unable to process shift type {ShiftType}");
                    }

                for (var ifx = -NScenarios; ifx < NScenarios + 1; ifx++)
                {
                    var fxIx = ifx + NScenarios;
                    var thisShiftFx = ifx * ShiftSizeFx;
                    var thisLabelFx = Ccy.Ccy + "~" + thisShiftFx;

                    IPvModel shiftedFx;

                    if (thisShiftAsset == 0)
                        shiftedFx = shifted;
                    else
                        shiftedFx = FlatShiftMutator.FxSpotShift(Ccy, thisShiftFx, shifted);

                    results[assetIx * axisLength + fxIx] = new KeyValuePair<Tuple<string, string>, IPvModel>(
                        new Tuple<string, string>(thisLabelAsset, thisLabelFx), shiftedFx);
                }
            }).Wait();

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }

        private Dictionary<Tuple<string, string>, IPvModel> GenerateScenariosFxFx(IPvModel model)
        {
            var o = new Dictionary<Tuple<string, string>, IPvModel>();
            var axisLength = NScenarios * 2 + 1;
            var results = new KeyValuePair<Tuple<string, string>, IPvModel>[axisLength * axisLength];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShiftFx1 = i * ShiftSizeAsset;
                var thisLabelAsset = Pair1.ToString() + "~" + thisShiftFx1;

                var assetIx = i + NScenarios;

                IPvModel shifted;

                if (thisShiftFx1 == 0)
                    shifted = model;
                else
                    switch (ShiftType)
                    {
                        case MutationType.FlatShift:
                            shifted = FlatShiftMutator.FxSpotShift(Pair1, thisShiftFx1, model);
                            break;
                        default:
                            throw new Exception($"Unable to process shift type {ShiftType}");
                    }

                for (var ifx = -NScenarios; ifx < NScenarios + 1; ifx++)
                {
                    var fxIx = ifx + NScenarios;
                    var thisShiftFx = ifx * ShiftSizeFx;
                    var thisLabelFx = Pair2.ToString() + "~" + thisShiftFx;

                    IPvModel shiftedFx;

                    if (thisShiftFx == 0)
                        shiftedFx = shifted;
                    else
                        shiftedFx = FlatShiftMutator.FxSpotShift(Pair2, thisShiftFx, shifted);

                    results[assetIx * axisLength + fxIx] = new KeyValuePair<Tuple<string, string>, IPvModel>(
                        new Tuple<string, string>(thisLabelAsset, thisLabelFx), shiftedFx);
                }
            }).Wait();

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }


        public ICube Generate(IPvModel model, Portfolio portfolio = null)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type>
            {
                { "AxisA", typeof(string) },
                { "AxisB", typeof(string) }
            });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;
            if (ReturnDifferential)
            {
                var baseModel = model;
                if (portfolio != null)
                {
                    baseModel = baseModel.Rebuild(baseModel.VanillaModel, portfolio);
                }
                baseRiskCube = GetRisk(baseModel);
            }

            var threadLock = new object();
            var results = new ICube[scenarios.Count];
            var scList = scenarios.ToList();

            ParallelUtils.Instance.For(0, scList.Count,1, i =>
            {
                var scenario = scList[i];
                var pvModel = scenario.Value;
                if (portfolio != null)
                {
                    pvModel = pvModel.Rebuild(pvModel.VanillaModel, portfolio);
                }
                var result = GetRisk(pvModel);

                if (ReturnDifferential)
                {
                    result = result.Difference(baseRiskCube);
                }

                results[i] = result;
            }).Wait();

            for (var i = 0; i < results.Length; i++)
            {
                o = (ResultCube)o.Merge(results[i], 
                    new Dictionary<string, object>
                    {
                        { "AxisA", scList[i].Key.Item1 },
                        { "AxisB", scList[i].Key.Item2 }
                    }, null, true);
            }

            return o;
        }

        private ICube GetRisk(IPvModel model)
        {
            switch (Metric)
            {
                case RiskMetric.AssetCurveDelta:
                    return model.AssetDelta();
                //case RiskMetric.AssetCurveDeltaGamma:
                //    return portfolio.AssetDeltaGamma(model);
                case RiskMetric.FxDelta:
                    return model.FxDeltaSpecific(_currencyProvider.GetCurrency("ZAR"), FxPairsForDelta, _currencyProvider, false);
                //case RiskMetric.FxDeltaGamma:
                //    return portfolio.FxDelta(model, _currencyProvider.GetCurrency("ZAR"), _currencyProvider, true);
                default:
                    throw new Exception($"Unable to process risk metric {Metric}");

            }
        }
    }
}
