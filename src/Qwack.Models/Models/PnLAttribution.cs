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
        public static ICube BasicAttribution(this Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency reportingCcy)
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

            //first step roll time fwd
            var model = startModel.RollModel(endModel.BuildDate);
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
    }
}
