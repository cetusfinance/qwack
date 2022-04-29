using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public class TimeLadder
    {
        private readonly ICurrencyProvider _currencyProvider;

        public RiskMetric Metric { get; private set; }
        public int NScenarios { get; private set; }
        public bool ReturnDifferential { get; private set; }
        public Calendar Calendar { get; private set; }

        public TimeLadder(RiskMetric metric, int nScenarios, Calendar calendar, ICurrencyProvider currencyProvider, bool returnDifferential = true)
        {
            Metric = metric;
            NScenarios = nScenarios;
            Calendar = calendar;
            _currencyProvider = currencyProvider;
            ReturnDifferential = returnDifferential;
        }

        public Dictionary<string, IPvModel> GenerateScenarios(IPvModel model)
        {
            var o = new Dictionary<string, IPvModel>();
            var shifts = new double[NScenarios + 1];
            var m = model.VanillaModel.Clone();
            var d = model.VanillaModel.BuildDate;
            var pd = d;
            for (var i = 0; i < NScenarios; i++)
            {
                var thisLabel = $"+{i} days";
                if (i == 0)
                    o.Add(thisLabel, model);
                else
                {
                    d = d.AddPeriod(RollType.F, Calendar, 1.Bd());
                    m = m.RollModel(d, _currencyProvider);
                    var newPvModel = model.Rebuild(m, model.Portfolio.RollWithLifecycle(d, pd));
                    o.Add(thisLabel, m);
                    pd = d;
                }
            }

            return o;
        }

        public ICube Generate(IPvModel model, Portfolio portfolio = null)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type> { { "Scenario", typeof(string) } });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;
            if (ReturnDifferential)
            {
                var baseModel = model;
                if (portfolio != null)
                {
                    baseModel = baseModel.Rebuild(baseModel.VanillaModel, portfolio.RollWithLifecycle(model.VanillaModel.BuildDate, model.VanillaModel.BuildDate));
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
                    pvModel = pvModel.Rebuild(pvModel.VanillaModel, portfolio.RollWithLifecycle(model.VanillaModel.BuildDate, pvModel.VanillaModel.BuildDate));
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

        private ICube GetRisk(IPvModel model) => Metric switch
        {
            RiskMetric.AssetCurveDelta => model.AssetDelta(),
            RiskMetric.FxDelta => model.FxDelta(model.VanillaModel.FundingModel.FxMatrix.BaseCurrency, _currencyProvider, false),
            _ => throw new Exception($"Unable to process risk metric {Metric}"),
        };
    }
}
