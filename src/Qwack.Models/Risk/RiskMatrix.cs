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

        public Dictionary<Tuple<string,string>,IAssetFxModel> GenerateScenarios(IAssetFxModel model)
        {
            var o = new Dictionary<Tuple<string, string>, IAssetFxModel>();
            var axisLength = NScenarios * 2 + 1;
            var results = new KeyValuePair<Tuple<string, string>, IAssetFxModel>[axisLength * axisLength];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShiftAsset = i * ShiftSizeAsset;
                var thisLabelAsset = AssetId + "~" + thisShiftAsset;

                var assetIx = i + NScenarios;

                IAssetFxModel shifted;

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

                    IAssetFxModel shiftedFx;

                    if (thisShiftAsset == 0)
                        shiftedFx = shifted;
                    else
                        shiftedFx = FlatShiftMutator.FxSpotShift(Ccy, thisShiftFx, shifted);

                    results[assetIx*axisLength+fxIx] = new KeyValuePair<Tuple<string, string>, IAssetFxModel>(
                        new Tuple<string, string>(thisLabelAsset, thisLabelFx), shiftedFx);
                }
            }).Wait();

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }

        public ICube Generate(IAssetFxModel model, Portfolio portfolio)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type>
            {
                { "AxisA", typeof(string) },
                { "AxisB", typeof(string) }
            });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;
            if(ReturnDifferential)
            {
                baseRiskCube = GetRisk(model, portfolio);
            }

            var threadLock = new object();
            var results = new ICube[scenarios.Count];
            var scList = scenarios.ToList();

            ParallelUtils.Instance.For(0, scList.Count,1, i =>
            {
                var scenario = scList[i];
                var result = GetRisk(scenario.Value, portfolio);

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

        private ICube GetRisk(IAssetFxModel model, Portfolio portfolio)
        {
            switch (Metric)
            {
                case RiskMetric.AssetCurveDelta:
                    return portfolio.AssetDelta(model);
                case RiskMetric.AssetCurveDeltaGamma:
                    return portfolio.AssetDeltaGamma(model);
                case RiskMetric.FxDelta:
                    return portfolio.FxDelta(model, _currencyProvider.GetCurrency("ZAR"), _currencyProvider);
                case RiskMetric.FxDeltaGamma:
                    return portfolio.FxDelta(model, _currencyProvider.GetCurrency("ZAR"), _currencyProvider, true);
                default:
                    throw new Exception($"Unable to process risk metric {Metric}");

            }
        }
    }
}
