using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Risk
{
    public static class BasicMetrics
    {

        public static Dictionary<string, ICube> ComputeBumpedScenarios(Func<IAssetFxModel,ICube> pvFunc, Dictionary<string, IAssetFxModel> models, Currency ccy)
        {
            var results = new Tuple<string, ICube>[models.Count];
            var bModelList = models.ToList();
            ParallelUtils.Instance.For(0, results.Length, 1, ii =>
            {
                var bModel = bModelList[ii];
                var bumpedPVCube = pvFunc.Invoke(bModel.Value);
                results[ii] = new Tuple<string, ICube>(bModel.Key, bumpedPVCube);
            }).Wait();
            return results.ToDictionary(k=>k.Item1,v=>v.Item2);
        }
    }
}
