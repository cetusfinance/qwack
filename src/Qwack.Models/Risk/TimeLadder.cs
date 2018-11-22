using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Models.Risk.Mutators;
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

        public TimeLadder(RiskMetric metric, int nScenarios, Calendar calendar, ICurrencyProvider currencyProvider, bool returnDifferential=true)
        {
            Metric = metric;
            NScenarios = nScenarios;
            Calendar = calendar;
            _currencyProvider = currencyProvider;
            ReturnDifferential = returnDifferential;
        }

        public Dictionary<string,IAssetFxModel> GenerateScenarios(IAssetFxModel model)
        {
            var o = new Dictionary<string, IAssetFxModel>();
            var shifts = new double[NScenarios + 1];
            var m = model.Clone();
            var d = model.BuildDate;
            for (var i=0;i<NScenarios;i++)
            {
                var thisLabel = $"+{i} days";
                if (i == 0)
                    o.Add(thisLabel, model);
                else
                {
                    d = d.AddPeriod(RollType.F, Calendar, 1.Bd());
                    m = m.RollModel(d, _currencyProvider);
                    o.Add(thisLabel, m);
                }
            }

            return o;
        }

        public ICube Generate(IAssetFxModel model, Portfolio portfolio)
        {
            var o = new ResultCube();
            o.Initialize(new Dictionary<string, Type> { { "Scenario", typeof(string) } });

            var scenarios = GenerateScenarios(model);

            ICube baseRiskCube = null;
            if(ReturnDifferential)
            {
                baseRiskCube = GetRisk(model, portfolio);
            }

            var threadLock = new object();
            var results = new ICube[scenarios.Count];
            var scList = scenarios.ToList();

            ParallelUtils.Instance.For(0, scList.Count, 1, i =>
            //foreach (var scenario in scenarios)
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
                    new Dictionary<string, object> { { "Scenario", scList[i].Key } }, null, true);
            }

            return o;
        }

        private ICube GetRisk(IAssetFxModel model, Portfolio portfolio)
        {
            switch (Metric)
            {
                case RiskMetric.AssetCurveDelta:
                    return portfolio.AssetDelta(model);
                default:
                    throw new Exception($"Unable to process risk metric {Metric}");

            }
        }
    }
}
