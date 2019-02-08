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
    public class RiskLadder
    {
        public string AssetId { get; private set; }
        public MutationType ShiftType { get; private set; }
        public RiskMetric Metric { get; private set; }
        public double ShiftSize { get; private set; }
        public int NScenarios { get; private set; }
        public bool ReturnDifferential { get; private set; }
        public Currency Ccy { get; private set; }

        public RiskLadder(string assetId, MutationType shiftType, RiskMetric metric, double shiftStepSize, int nScenarios, bool returnDifferential=true)
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

        public Dictionary<string, IPvModel> GenerateScenarios(IPvModel model)
        {
            var o = new Dictionary<string, IPvModel>();

            var results = new KeyValuePair<string, IPvModel>[NScenarios * 2 + 1];
            ParallelUtils.Instance.For(-NScenarios, NScenarios + 1, 1, (i) =>
            {
                var thisShift = i * ShiftSize;
                var thisLabel = (string.IsNullOrWhiteSpace(AssetId) ? Ccy.Ccy : AssetId) + "~" + thisShift;
                if (thisShift == 0)
                    results[i+NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, model);
                else
                {
                    if (string.IsNullOrWhiteSpace(AssetId))
                    {
                        IPvModel shifted;
                        switch (ShiftType)
                        {
                            case MutationType.FlatShift:
                                shifted = FlatShiftMutator.FxSpotShift(Ccy, thisShift, model);
                                break;
                            default:
                                throw new Exception($"Unable to process shift type {ShiftType}");
                        }
                        results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                    else
                    {
                        IPvModel shifted;
                        switch (ShiftType)
                        {
                            case MutationType.FlatShift:
                                shifted = FlatShiftMutator.AssetCurveShift(AssetId, thisShift, model);
                                break;
                            default:
                                throw new Exception($"Unable to process shift type {ShiftType}");
                        }
                        results[i + NScenarios] = new KeyValuePair<string, IPvModel>(thisLabel, shifted);
                    }
                }
            }).Wait();

            foreach (var kv in results)
                o.Add(kv.Key, kv.Value);

            return o;
        }

        public ICube Generate(IPvModel model, Portfolio portfolio = null)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type> { { "Scenario", typeof(string) } });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;
            
            if(ReturnDifferential)
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
                    new Dictionary<string, object> { { "Scenario", scList[i].Key } }, null, true);
            }

            return o;
        }

        private ICube GetRisk(IPvModel model)
        {
            switch (Metric)
            {
                case RiskMetric.AssetCurveDelta:
                    return model.AssetDeltaSingleCurve(AssetId);
                case RiskMetric.AssetVega:
                    return model.AssetVega(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency);
                default:
                    throw new Exception($"Unable to process risk metric {Metric}");

            }
        }
    }
}
