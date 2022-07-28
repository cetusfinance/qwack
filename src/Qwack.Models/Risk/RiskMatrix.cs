using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
using Qwack.Transport.BasicTypes;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class RiskMatrix
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendar;

        public string AssetId { get; private set; }
        public Currency Ccy { get; private set; }
        public MutationType ShiftType { get; private set; }
        public RiskMetric Metric { get; private set; }
        public double ShiftSizeAsset { get; private set; }
        public double ShiftSizeFx { get; private set; }
        public int NScenarios { get; private set; }
        public int? NTimeSteps { get; private set; }
        public bool ReturnDifferential { get; private set; }

        public FxPair Pair1 { get; private set; }
        public FxPair Pair2 { get; private set; }

        public List<FxPair> FxPairsForDelta { get; set; }

        public RiskMatrix(string assetId, Currency ccy, MutationType shiftType, RiskMetric metric, double shiftStepSizeAsset, double shiftStepSizeFx, int nScenarios, ICurrencyProvider currencyProvider, bool returnDifferential = true)
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

        public RiskMatrix(string assetId, MutationType shiftType, RiskMetric metric, double shiftStepSizeAsset, int nTimeSteps, int nScenarios, ICurrencyProvider currencyProvider, ICalendarProvider calendar, bool returnDifferential = true)
        {
            AssetId = assetId;
            NTimeSteps = nTimeSteps;
            ShiftType = shiftType;
            Metric = metric;
            ShiftSizeAsset = shiftStepSizeAsset;
            NScenarios = nScenarios;
            _currencyProvider = currencyProvider;
            _calendar = calendar;
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

        public Dictionary<Tuple<string, string>, IPvModel> GenerateScenarios(IPvModel model)
        {
            if (!string.IsNullOrEmpty(AssetId))
            {
                if (NTimeSteps.HasValue)
                    return GenerateScenariosAssetTime(model);
                else
                    return GenerateScenariosAssetFx(model);
            }
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
                    shifted = ShiftType switch
                    {
                        MutationType.FlatShift => FlatShiftMutator.AssetCurveShift(AssetId, thisShiftAsset, model),
                        _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                    };
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

        private Dictionary<Tuple<string, string>, IPvModel> GenerateScenariosAssetTime(IPvModel model)
        {
            var o = new Dictionary<Tuple<string, string>, IPvModel>();
            var axisLength = NScenarios * 2 + 1;
            var results = new KeyValuePair<Tuple<string, string>, IPvModel>[axisLength * NTimeSteps.Value];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShiftAsset = i * ShiftSizeAsset;
                var thisLabelAsset = AssetId + "~" + thisShiftAsset;

                var assetIx = i + NScenarios;

                IPvModel shifted;

                if (thisShiftAsset == 0)
                    shifted = model;
                else
                    shifted = ShiftType switch
                    {
                        MutationType.FlatShift => FlatShiftMutator.AssetCurveShift(AssetId, thisShiftAsset, model),
                        _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                    };

                var d = model.VanillaModel.BuildDate;
                var pd = d;
                for (var iT = 0; iT < NTimeSteps.Value; iT++)
                {
                    var thisLabelTime = $"+{iT} days";

                    IPvModel shiftedTime;

                    d = d.AddPeriod(RollType.F, _calendar.GetCalendarSafe("USD"), iT.Bd());
                    shiftedTime = shifted.VanillaModel.Clone().RollModel(d, _currencyProvider);
                    shiftedTime = shifted.Rebuild(shiftedTime.VanillaModel, model.Portfolio.RollWithLifecycle(d, pd));
                    pd = d;

                    results[assetIx * NTimeSteps.Value + iT] = new KeyValuePair<Tuple<string, string>, IPvModel>(
                        new Tuple<string, string>(thisLabelAsset, thisLabelTime), shiftedTime);
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
                    shifted = ShiftType switch
                    {
                        MutationType.FlatShift => FlatShiftMutator.FxSpotShift(Pair1, thisShiftFx1, model),
                        _ => throw new Exception($"Unable to process shift type {ShiftType}"),
                    };
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

            ParallelUtils.Instance.For(0, scList.Count, 1, i =>
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

        private ICube GetRisk(IPvModel model) => Metric switch
        {
            RiskMetric.AssetCurveDelta => model.AssetDelta(),
            RiskMetric.PV => model.PV(Ccy??model.VanillaModel.FundingModel.FxMatrix.BaseCurrency),
            //case RiskMetric.AssetCurveDeltaGamma:
            //    return portfolio.AssetDeltaGamma(model);
            RiskMetric.FxDelta => model.FxDeltaSpecific(_currencyProvider.GetCurrency("ZAR"), FxPairsForDelta, _currencyProvider, false),
            //case RiskMetric.FxDeltaGamma:
            //    return portfolio.FxDelta(model, _currencyProvider.GetCurrency("ZAR"), _currencyProvider, true);
            _ => throw new Exception($"Unable to process risk metric {Metric}"),
        };
    }
}
