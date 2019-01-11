using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;

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
                { "TradeType", typeof(string) },
                { "Step", typeof(string) },
                { "SubStep", typeof(string) },
            };
            cube.Initialize(dataTypes);

            var pvCubeBase = portfolio.PV(startModel, reportingCcy);
            var pvRows = pvCubeBase.GetAllRows();
            var tidIx = pvCubeBase.GetColumnIndex("TradeId");
            var tTypeIx = pvCubeBase.GetColumnIndex("TradeType");

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
                    { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", cashRows[i].MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                        { "TradeType", r.MetaData[tTypeIx] },
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
                { "TradeType", typeof(string) },
                { "Step", typeof(string) },
                { "SubStep", typeof(string) },
                { "SubSubStep", typeof(string) },
                { "PointLabel", typeof(string) },
            };

            cube.Initialize(dataTypes);

            var pvCubeBase = portfolio.PV(startModel, reportingCcy);
            var pvRows = pvCubeBase.GetAllRows();
            var tidIx = pvCubeBase.GetColumnIndex("TradeId");
            var tTypeIx = pvCubeBase.GetColumnIndex("TradeType");

            var cashCube = portfolio.FlowsT0(startModel, reportingCcy);
            var cashRows = cashCube.GetAllRows();

            var r_tidIx = startingGreeks.GetColumnIndex("TradeId");
            var r_plIx = startingGreeks.GetColumnIndex("PointLabel");
            var r_tTypeIx = startingGreeks.GetColumnIndex("TradeType");

            //first step roll time fwd
            var model = startModel.RollModel(endModel.BuildDate, currencyProvider);
            var newPVCube = portfolio.PV(model, reportingCcy);

            var step = newPVCube.QuickDifference(pvCubeBase);
            foreach (var r in step.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", r.MetaData[tidIx] },
                    { "TradeType", r.MetaData[tTypeIx] },
                    { "Step", "Theta" },
                    { "SubStep", string.Empty },
                    { "SubSubStep", string.Empty },
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
                        { "TradeType", cashRows[i].MetaData[tTypeIx] },
                        { "Step", "Theta" },
                        { "SubStep", "CashMove" },
                        { "SubSubStep", string.Empty },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "Fixings" },
                        { "SubStep", fixingDictName },
                        { "SubSubStep", string.Empty },
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
                    var point = DateTime.Parse((string)r.MetaData[r_plIx]);
                    var startRate = model.FundingModel.Curves[irCurve.Key].GetRate(point);
                    var endRate = irCurve.Value.GetRate(point);
                    var explained = r.Value * (endRate - startRate) / 0.0001;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", string.Empty },
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
                    if (explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained))
                    {
                        explainedByTrade.Remove((string)r.MetaData[tidIx]);
                    }

                    if (r.Value - explained == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", "Unexplained"},
                        { "PointLabel", "Unexplained" }
                    };
                    cube.AddRow(row, r.Value - explained);
                }

                //overspill
                foreach (var kv in explainedByTrade)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", kv.Key },
                        { "TradeType", string.Empty },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", "Unexplained"},
                        { "PointLabel", "Unexplained" }
                    };
                    cube.AddRow(row, -kv.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move fx spots
            foreach (var fxSpot in endModel.FundingModel.FxMatrix.SpotRates)
            {
                var fxPair = $"{endModel.FundingModel.FxMatrix.BaseCurrency.Ccy}/{fxSpot.Key.Ccy}";

                //delta
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
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Delta" },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                //gamma
                riskForCurve = startingGreeks.Filter(
                   new Dictionary<string, object> {
                        { "AssetId", fxPair },
                        { "Metric", "FxSpotGammaT1" }
                   });
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var startRate = model.FundingModel.FxMatrix.SpotRates[fxSpot.Key];
                    var endRate = fxSpot.Value;
                    var explained = r.Value * (endRate - startRate) * (endRate - startRate) * 0.5;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Gamma" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Unexplained" },
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
                var riskForCurveGamma = startingGreeks.Filter(
                  new Dictionary<string, object> {
                        { "AssetId", curveName },
                        { "Metric", "AssetGammaT1" }
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
                    var move = (endRate - startRate);
                    var explained = r.Value * move * fxRate;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Delta" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                foreach (var r in riskForCurveGamma.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];

                    var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var move = (endRate - startRate);
                    var explained = 0.5 * r.Value * move * move * fxRate;


                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Gamma" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Unexplained" },
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

                var startCurve = startModel.GetVolSurface(surfaceName);
                var endCurve = endModel.GetVolSurface(surfaceName);
                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = startCurve.PillarDatesForLabel(point);
                    var startRate = model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var endRate = endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.01;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Vega" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                var targetSurface = endModel.GetVolSurface(surfaceName);
                model.AddVolSurface(surfaceName, targetSurface);
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Unexplained" },
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
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "SubSubStep", "Vega" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "SubSubStep", "Unexplained" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "Unexplained" },
                        { "SubStep", "Unexplained" },
                        { "SubSubStep", "Unexplained" },
                        { "PointLabel", string.Empty }
                    };
                cube.AddRow(row, r.Value);
            }
            lastPVCuve = newPVCube;

            return cube;
        }

        public static ICube ExplainAttributionInLineGreeks(this Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "TradeType", typeof(string) },
                { "Step", typeof(string) },
                { "SubStep", typeof(string) },
                { "SubSubStep", typeof(string) },
                { "PointLabel", typeof(string) },
            };

            cube.Initialize(dataTypes);

            var pvCubeBase = portfolio.PV(startModel, reportingCcy);
            var pvRows = pvCubeBase.GetAllRows();
            var tidIx = pvCubeBase.GetColumnIndex("TradeId");
            var tTypeIx = pvCubeBase.GetColumnIndex("TradeType");

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
                    { "TradeType", r.MetaData[tTypeIx] },
                    { "Step", "Theta" },
                    { "SubStep", "TimeMove" },
                    { "SubSubStep", string.Empty },
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
                        { "TradeType", cashRows[i].MetaData[tTypeIx] },
                        { "Step", "Theta" },
                        { "SubStep", "CashMove" },
                        { "SubSubStep", string.Empty },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "Fixings" },
                        { "SubStep", fixingDictName },
                        { "SubSubStep", string.Empty },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, r.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move ir curves
            var irGreeks = portfolio.AssetIrDelta(model, reportingCcy);
            var r_tidIx = irGreeks.GetColumnIndex("TradeId");
            var r_plIx = irGreeks.GetColumnIndex("PointLabel");
            var r_tTypeIx = irGreeks.GetColumnIndex("TradeType");
            foreach (var irCurve in endModel.FundingModel.Curves)
            {
                var riskForCurve = irGreeks.Filter(
                    new Dictionary<string, object> {
                        { "AssetId", irCurve.Key },
                        { "Metric", "IrDelta" }
                    });

                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = DateTime.Parse((string)r.MetaData[r_plIx]);
                    var startRate = model.FundingModel.Curves[irCurve.Key].GetRate(point);
                    var endRate = irCurve.Value.GetRate(point);
                    var explained = r.Value * (endRate - startRate) / 0.0001;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", string.Empty },
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
                    if (explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained))
                    {
                        explainedByTrade.Remove((string)r.MetaData[tidIx]);
                    }

                    if (r.Value - explained == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", "Unexplained"},
                        { "PointLabel", "Unexplained" }
                    };
                    cube.AddRow(row, r.Value - explained);
                }

                //overspill
                foreach (var kv in explainedByTrade)
                {
                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", kv.Key },
                        { "TradeType", string.Empty },
                        { "Step", "IrCurves" },
                        { "SubStep", irCurve.Key },
                        { "SubSubStep", "Unexplained"},
                        { "PointLabel", "Unexplained" }
                    };
                    cube.AddRow(row, -kv.Value);
                }

                lastPVCuve = newPVCube;
            }

            //next move fx spots
            var fxGreeks = portfolio.FxDelta(model, reportingCcy, currencyProvider, true);
            r_tidIx = fxGreeks.GetColumnIndex("TradeId");
            r_tTypeIx = fxGreeks.GetColumnIndex("TradeType");
            foreach (var fxSpot in endModel.FundingModel.FxMatrix.SpotRates)
            {
                var fxPair = $"{endModel.FundingModel.FxMatrix.BaseCurrency.Ccy}/{fxSpot.Key.Ccy}";

                //delta
                var riskForCurve = fxGreeks.Filter(
                    new Dictionary<string, object> {
                        { "AssetId", fxPair },
                        { "Metric", "FxSpotDelta" }
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
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Delta" },
                        { "PointLabel", string.Empty }
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                //gamma
                riskForCurve = fxGreeks.Filter(
                   new Dictionary<string, object> {
                        { "AssetId", fxPair },
                        { "Metric", "FxSpotGamma" }
                   });
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var startRate = model.FundingModel.FxMatrix.SpotRates[fxSpot.Key];
                    var endRate = fxSpot.Value;
                    var explained = r.Value * (endRate - startRate) * (endRate - startRate) * 0.5;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Gamma" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "FxSpots" },
                        { "SubStep", fxPair },
                        { "SubSubStep", "Unexplained" },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset curves
            var curveGreeks = portfolio.AssetDeltaGamma(model);
            r_tidIx = curveGreeks.GetColumnIndex("TradeId");
            r_plIx = curveGreeks.GetColumnIndex("PointLabel");
            r_tTypeIx = curveGreeks.GetColumnIndex("TradeType");
            foreach (var curveName in endModel.CurveNames)
            {
                var riskForCurve = curveGreeks.Filter(
                   new Dictionary<string, object> {
                        { "AssetId", curveName },
                        { "Metric", "Delta" }
                   });
                var riskForCurveGamma = curveGreeks.Filter(
                  new Dictionary<string, object> {
                        { "AssetId", curveName },
                        { "Metric", "Gamma" }
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
                    var move = (endRate - startRate);
                    var explained = r.Value * move * fxRate;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Delta" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                foreach (var r in riskForCurveGamma.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];

                    var startRate = startCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var endRate = endCurve.GetPriceForDate(startCurve.PillarDatesForLabel(point));
                    var move = (endRate - startRate);
                    var explained = 0.5 * r.Value * move * move * fxRate;


                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Gamma" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "AssetCurves" },
                        { "SubStep", curveName },
                        { "SubSubStep", "Unexplained" },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move asset vols
            var assetVega = portfolio.AssetVega(model, reportingCcy);
            var assetSegaRega = portfolio.AssetSegaRega(model, reportingCcy);
            r_tidIx = assetVega.GetColumnIndex("TradeId");
            r_plIx = assetVega.GetColumnIndex("PointLabel");
            r_tTypeIx = assetVega.GetColumnIndex("TradeType");
            foreach (var surfaceName in endModel.VolSurfaceNames)
            {
                //ATM vega
                var riskForCurve = assetVega.Filter(
                      new Dictionary<string, object> {
                        { "AssetId", surfaceName },
                        { "Metric", "Vega" }
                      });

                var startCurve = startModel.GetVolSurface(surfaceName);
                var endCurve = endModel.GetVolSurface(surfaceName);
                var explainedByTrade = new Dictionary<string, double>();
                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = startCurve.PillarDatesForLabel(point);
                    var startRate = model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var endRate = endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.01;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Vega" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                //Rega
                 riskForCurve = assetSegaRega.Filter(
                      new Dictionary<string, object> {
                        { "AssetId", surfaceName },
                        { "Metric", "Rega" }
                      });

                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = startCurve.PillarDatesForLabel(point);
                    var startRate = model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.75)- model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.25);
                    var endRate = endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.75)- endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.25); ;
                    var explained = r.Value * (endRate - startRate) / 0.001;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Rega" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                //Sega
                riskForCurve = assetSegaRega.Filter(
                     new Dictionary<string, object> {
                        { "AssetId", surfaceName },
                        { "Metric", "Sega" }
                     });

                foreach (var r in riskForCurve.GetAllRows())
                {
                    if (r.Value == 0.0) continue;
                    var point = (string)r.MetaData[r_plIx];
                    var pointDate = startCurve.PillarDatesForLabel(point);
                    var startRate = (model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.75) + model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.25))/2.0 - model.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var endRate = (endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.75) + endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.25)) / 2.0 - endModel.GetVolForDeltaStrikeAndDate(surfaceName, pointDate, 0.5);
                    var explained = r.Value * (endRate - startRate) / 0.001;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[r_tidIx] },
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Sega" },
                        { "PointLabel",r.MetaData[r_plIx]}
                    };
                    cube.AddRow(row, explained);

                    if (!explainedByTrade.ContainsKey((string)r.MetaData[r_tidIx]))
                        explainedByTrade[(string)r.MetaData[r_tidIx]] = explained;
                    else
                        explainedByTrade[(string)r.MetaData[r_tidIx]] += explained;
                }

                //UX
                var targetSurface = endModel.GetVolSurface(surfaceName);
                model.AddVolSurface(surfaceName, targetSurface);
                newPVCube = portfolio.PV(model, reportingCcy);
                step = newPVCube.QuickDifference(lastPVCuve);

                foreach (var r in step.GetAllRows())
                {
                    if (r.Value == 0.0) continue;

                    var row = new Dictionary<string, object>
                    {
                        { "TradeId", r.MetaData[tidIx] },
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "AssetVols" },
                        { "SubStep", surfaceName },
                        { "SubSubStep", "Unexplained" },
                        { "PointLabel", "Unexplained" }
                    };
                    explainedByTrade.TryGetValue((string)r.MetaData[tidIx], out var explained);
                    cube.AddRow(row, r.Value - explained);
                }
                lastPVCuve = newPVCube;
            }

            //next move fx vols
            var fxVega = portfolio.FxVega(model, reportingCcy);
            r_tidIx = fxVega.GetColumnIndex("TradeId");
            r_plIx = fxVega.GetColumnIndex("PointLabel");
            r_tTypeIx = fxVega.GetColumnIndex("TradeType");
            foreach (var fxSurface in endModel.FundingModel.VolSurfaces)
            {
                var riskForCurve = fxVega.Filter(
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
                        { "TradeType", r.MetaData[r_tTypeIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "SubSubStep", "Vega" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "FxVols" },
                        { "SubStep", fxSurface.Key },
                        { "SubSubStep", "Unexplained" },
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
                        { "TradeType", r.MetaData[tTypeIx] },
                        { "Step", "Unexplained" },
                        { "SubStep", "Unexplained" },
                        { "SubSubStep", "Unexplained" },
                        { "PointLabel", string.Empty }
                    };
                cube.AddRow(row, r.Value);
            }
            lastPVCuve = newPVCube;

            return cube;
        }


        public static ICube ExplainAttribution(this Portfolio startPortfolio, Portfolio endPortfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency reportingCcy, ICurrencyProvider currencyProvider)
        {
            //first do normal attribution
            var cube = startPortfolio.ExplainAttributionInLineGreeks(startModel, endModel, reportingCcy, currencyProvider);

            //then do activity PnL
            var (newTrades, removedTrades, ammendedTradesStart, ammendedTradesEnd) = startPortfolio.ActivityBooks(endPortfolio, endModel.BuildDate);

            var newTradesPnL = newTrades.PV(endModel, reportingCcy);
            var tidIx = newTradesPnL.GetColumnIndex("TradeId");
            var tTypeIx = newTradesPnL.GetColumnIndex("TradeType");
            foreach (var t in newTradesPnL.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", t.MetaData[tidIx] },
                    { "TradeType", t.MetaData[tTypeIx] },
                    { "Step", "Activity" },
                    { "SubStep", "New" },
                    { "SubSubStep", string.Empty },
                    { "PointLabel", string.Empty }
                };
                cube.AddRow(row, t.Value);
            }

            var removedTradesPnL = removedTrades.PV(endModel, reportingCcy);
            foreach (var t in removedTradesPnL.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", t.MetaData[tidIx] },
                    { "TradeType", t.MetaData[tTypeIx] },
                    { "Step", "Activity" },
                    { "SubStep", "Removed" },
                    { "SubSubStep", string.Empty },
                    { "PointLabel", string.Empty }
                };
                cube.AddRow(row, -t.Value);
            }

            var ammendedTradesPnLStart = ammendedTradesStart.PV(endModel, reportingCcy);
            var ammendedTradesPnLEnd = ammendedTradesEnd.PV(endModel, reportingCcy);
            var ammendedPnL = ammendedTradesPnLEnd.QuickDifference(ammendedTradesPnLStart);
            foreach (var t in ammendedPnL.GetAllRows())
            {
                var row = new Dictionary<string, object>
                {
                    { "TradeId", t.MetaData[tidIx] },
                    { "TradeType", t.MetaData[tTypeIx] },
                    { "Step", "Activity" },
                    { "SubStep", "Ammended" },
                    { "SubSubStep", string.Empty },
                    { "PointLabel", string.Empty }
                };
                cube.AddRow(row, t.Value);
            }

            return cube;
        }
    }
}
