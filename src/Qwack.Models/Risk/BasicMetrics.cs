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

        public static Dictionary<string, ICube> ComputeBumpedScenarios(Dictionary<string, IPvModel> models, Currency ccy)
        {
            var results = new Tuple<string, ICube>[models.Count];
            var bModelList = models.ToList();
            ParallelUtils.Instance.For(0, results.Length, 1, ii =>
            {
                var bModel = bModelList[ii];
                var bumpedPVCube = bModel.Value.PV(ccy);
                results[ii] = new Tuple<string, ICube>(bModel.Key, bumpedPVCube);
            }).Wait();
            return results.ToDictionary(k => k.Item1, v => v.Item2);
        }

        public static ICube AssetVega(this IPvModel pvModel, Currency reportingCcy)
        {
            var bumpSize = 0.01;
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "AssetId", typeof(string) },
                { "PointDate", typeof(DateTime) },
                { "PointLabel", typeof(string) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            var model = pvModel.VanillaModel;

            foreach (var surfaceName in model.VolSurfaceNames)
            {
                var volObj = model.GetVolSurface(surfaceName);

                var subPortfolio = new Portfolio()
                {
                    Instruments = model.Portfolio.Instruments.Where(x => (x is IHasVega) && (x is IAssetInstrument ia) && ia.AssetIds.Contains(volObj.AssetId)).ToList()
                };

                if (subPortfolio.Instruments.Count == 0)
                    continue;
                
                var lastDateInBook = subPortfolio.LastSensitivityDate();

                var basePvModel = pvModel.Rebuild(model, subPortfolio);
                var pvCube = basePvModel.PV(reportingCcy); 
                var pvRows = pvCube.GetAllRows();
                var tidIx = pvCube.GetColumnIndex("TradeId");

                var bumpedSurfaces = volObj.GetATMVegaScenarios(bumpSize, lastDateInBook);

                ParallelUtils.Instance.Foreach(bumpedSurfaces.ToList(), bCurve =>
                //foreach (var bCurve in bumpedSurfaces)
                {
                    var newVanillaModel = model.Clone();
                    newVanillaModel.AddVolSurface(surfaceName, bCurve.Value);
                    var bumpedPvModel = basePvModel.Rebuild(newVanillaModel, subPortfolio);
                    var bumpedPVCube = bumpedPvModel.PV(reportingCcy);
                    var bumpedRows = bumpedPVCube.GetAllRows();
                    if (bumpedRows.Length != pvRows.Length)
                        throw new Exception("Dimensions do not match");
                    
                    for (var i = 0; i < bumpedRows.Length; i++)
                    {
                        //vega quoted for a 1% shift, irrespective of bump size
                        var vega = (bumpedRows[i].Value - pvRows[i].Value) / bumpSize * 0.01;
                        if (vega != 0.0)
                        {
                            var row = new Dictionary<string, object>
                            {
                                { "TradeId", bumpedRows[i].MetaData[tidIx] },
                                { "AssetId", surfaceName },
                                { "PointDate", bCurve.Value.PillarDatesForLabel(bCurve.Key) },
                                { "PointLabel", bCurve.Key },
                                { "Metric", "Vega" }
                            };
                            cube.AddRow(row, vega);
                        }
                    }
                }, false).Wait();
            }

            return cube.Sort();
        }
    }
}
