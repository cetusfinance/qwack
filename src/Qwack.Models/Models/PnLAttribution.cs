using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;

namespace Qwack.Models.Models
{
    public static class PnLAttribution
    {
        public static ICube BasicAttribution(this Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Step", typeof(string) },
                { "SubStep", typeof(string) },
            };
            cube.Initialize(dataTypes);

            var pvCubeBase = portfolio.PV(startModel, reportingCcy);
            var pvRows = pvCubeBase.GetAllRows();
            var tidIx = pvCubeBase.GetColumnIndex("TradeId");

            var cashCube = portfolio.FlowsT0(startModel, reportingCcy);
            var cashRows = cashCube.GetAllRows();


            //first step roll time fwd
            var model = startModel.RollModel(endModel.BuildDate, currencyProvider);
            var newPVCube = portfolio.PV(model, reportingCcy);

            var step = newPVCube.QuickDifference(pvCubeBase);
            foreach (var r in step.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", r.MetaData[tidIx] },
                    { "Step", "Theta" },
                    { "SubStep", string.Empty }
                };
                cube.AddRow(row, r.Value);
            }
            var lastPVCuve = newPVCube;

            //next cash move
            for (var i = 0; i < cashRows.Length; i++)
            {
                var cash = cashRows[i].Value;
                if (cash != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", cashRows[i].MetaData[tidIx] },
                        { "Step", "Theta" },
                        { "SubStep", "CashMove" }
                    };
                    cube.AddRow(row, cash);
                }
            }

            //next replace fixings with actual values
            foreach (var fixingDictName in endModel.FixingDictionaryNames)
            {
                model.AddFixingDictionary(fixingDictName, endModel.GetFixingDictionary(fixingDictName));
                newPVCube = portfolio.PV(model, reportingCcy);

                step = newPVCube.QuickDifference(lastPVCuve);
                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "Fixings" },
                        { "SubStep", fixingDictName }
                    };
                    cube.AddRow(row, r.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move ir curves
            foreach (var irCurve in endModel.FundingModel.Curves)
            {
                model.FundingModel.Curves[irCurve.Key] = irCurve.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key }
                    };
                    cube.AddRow(row, r.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move fx spots
            foreach (var fxSpot in endModel.FundingModel.FxMatrix.SpotRates)
            {
                model.FundingModel.FxMatrix.SpotRates[fxSpot.Key] = fxSpot.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxSpot.Key.Ccy }
                    };
                    cube.AddRow(row, r.Value);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset curves
            foreach (var curveName in endModel.CurveNames)
            {
                model.AddPriceCurve(curveName, endModel.GetPriceCurve(curveName));
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName }
                    };
                    cube.AddRow(row, r.Value);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset vols
            foreach (var surfaceName in endModel.VolSurfaceNames)
            {
                model.AddVolSurface(surfaceName, endModel.GetVolSurface(surfaceName));
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName }
                    };
                    cube.AddRow(row, r.Value);
                }
                lastPVCuve = newPVCube;
            }

            //next move fx vols
            foreach (var fxSurface in endModel.FundingModel.VolSurfaces)
            {
                model.FundingModel.VolSurfaces[fxSurface.Key] = fxSurface.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key }
                    };
                    cube.AddRow(row, r.Value);
                }
                lastPVCuve = newPVCube;
            }

            //finally unexplained step
            newPVCube = portfolio.PV(endModel, reportingCcy);
            step = newPVCube.QuickDifference(lastPVCuve);

            foreach (var r in step.GetAllRows())
            {
                if (r.Value == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "Unexplained" },
                        { "SubStep", string.Empty }
                    };
                cube.AddRow(row, r.Value);
            }
            lastPVCuve = newPVCube;

            return cube;
        }

        public static ICube ExplainAttribution(this Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency reportingCcy, ICube startingGreeks, ICurrencyProvider currencyProvider)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Step", typeof(string) },
                { "SubStep", typeof(string) },
                { "PointLabel", typeof(string) },
            };

            cube.Initialize(dataTypes);

            var pvCubeBase = portfolio.PV(startModel, reportingCcy);
            var pvRows = pvCubeBase.GetAllRows();
            var tidIx = pvCubeBase.GetColumnIndex("TradeId");

            var cashCube = portfolio.FlowsT0(startModel, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var r_tidIx = startingGreeks.GetColumnIndex("TradeId");
            var r_plIx = startingGreeks.GetColumnIndex("PointLabel");

            //first step roll time fwd
            var model = startModel.RollModel(endModel.BuildDate, currencyProvider);
            var newPVCube = portfolio.PV(model, reportingCcy);

            var step = newPVCube.QuickDifference(pvCubeBase);
            foreach (var r in step.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", r.MetaData[tidIx] },
                    { "Step", "Theta" },
                    { "SubStep", string.Empty },
                    { "PointLabel", string.Empty }
                };
                cube.AddRow(row, r.Value);
            }
            var lastPVCuve = newPVCube;

            //next cash move
            for (var i = 0; i < cashRows.Length; i++)
            {
                var cash = cashRows[i].Value;
                if (cash != 0.0)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", cashRows[i].MetaData[tidIx] },
                        { "Step", "Theta" },
                        { "SubStep", "CashMove" },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, cash);
                }
            }

            //next replace fixings with actual values
            foreach (var fixingDictName in endModel.FixingDictionaryNames)
            {
                model.AddFixingDictionary(fixingDictName, endModel.GetFixingDictionary(fixingDictName));
                newPVCube = portfolio.PV(model, reportingCcy);

                step = newPVCube.QuickDifference(lastPVCuve);
                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "Fixings" },
                        { "SubStep", fixingDictName },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, r.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move ir curves
            foreach (var irCurve in endModel.FundingModel.Curves)
            {
                var riskForCurve = startingGreeks.Filter(
                    new Dictionary<string, object> {
                        { "AssetId", irCurve.Key },
                        { "Metric", "IrDelta" }
                    });

                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (DateTime)r.MetaData[r_plIx];
                    var startRate = model.FundingModel.Curves[irCurve.Key].GetRate(point);
                    var endRate = endModel.FundingModel.Curves[irCurve.Key].GetRate(point);
                    var explained = r.Value * (endRate - startRate) / 0.0001;
                        
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                model.FundingModel.Curves[irCurve.Key] = irCurve.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }

                lastPVCuve = newPVCube;
            }

            //next move fx spots
            foreach (var fxSpot in endModel.FundingModel.FxMatrix.SpotRates)
            {
                var fxPair = $"{endModel.FundingModel.FxMatrix.BaseCurrency.Ccy}/{fxSpot.Key.Ccy}";
                var riskForCurve = startingGreeks.Filter(
                    new Dictionary<string, object> {
                        { "AssetId", fxPair },
                        { "Metric", "FxSpotDeltaT1" }
                    });
                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var startRate = model.FundingModel.FxMatrix.SpotRates[fxSpot.Key];
                    var endRate = fxSpot.Value;
                    var explained = r.Value * (endRate - startRate);

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                model.FundingModel.FxMatrix.SpotRates[fxSpot.Key] = fxSpot.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset curves
            foreach (var curveName in endModel.CurveNames)
            {
                var riskForCurve = startingGreeks.Filter(
                   new Dictionary<string, object> {
                        { "AssetId", curveName },
                        { "Metric", "AssetDeltaT1" }
                   });

                var startCurve = model.GetPriceCurve(curveName);
                var endCurve = endModel.GetPriceCurve(curveName);

                var fxRate = model.FundingModel.GetFxRate(model.BuildDate, startCurve.Currency, reportingCcy);

                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    
                    var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var explained = r.Value * (endRate - startRate) * fxRate;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                model.AddPriceCurve(curveName, endModel.GetPriceCurve(curveName));
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset vols
            foreach (var surfaceName in endModel.VolSurfaceNames)
            {
                var riskForCurve = startingGreeks.Filter(
                      new Dictionary<string, object> {
                        { "AssetId", surfaceName },
                        { "Metric", "Vega" }
                      });

                var endCurve = endModel.GetVolSurface(surfaceName);
                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = endCurve.PillarDatesForLabel(point);
                    var startRate = model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var endRate = endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.01;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                model.AddVolSurface(surfaceName, endModel.GetVolSurface(surfaceName));
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move fx vols
            foreach (var fxSurface in endModel.FundingModel.VolSurfaces)
            {
                var riskForCurve = startingGreeks.Filter(
                     new Dictionary<string, object> {
                        { "AssetId", fxSurface.Key },
                        { "Metric", "Vega" }
                     });

                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = fxSurface.Value.PillarDatesForLabel(point);
                    var startRate = model.GetFxVolForDeltaStrikeAndDate(fxSurface.Key, pointDate, 0.5);
                    var endRate = endModel.GetFxVolForDeltaStrikeAndDate(fxSurface.Key, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.01;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                model.FundingModel.VolSurfaces[fxSurface.Key] = fxSurface.Value;
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //finally unexplained step
            newPVCube = portfolio.PV(endModel, reportingCcy);
            step = newPVCube.QuickDifference(lastPVCuve);

            foreach (var r in step.GetAllRows())
            {
                if (r.Value == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "Step", "Unexplained" },
                        { "SubStep", string.Empty },
                        { "PointLabel", string.Empty }
                    };
                cube.AddRow(row, r.Value);
            }
            lastPVCuve = newPVCube;

            return cube;
        }
    }
}
