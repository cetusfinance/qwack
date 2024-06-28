using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Models.MCModels;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class TimeRollStep(ICurrencyProvider currencyProvider, IFutureSettingsProvider futureSettingsProvider, ICalendarProvider calendarProvider) : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var r_tidIx = riskCube.GetColumnIndex(TradeId);
        var r_plIx = riskCube.GetColumnIndex(PointLabel);
        var r_tTypeIx = riskCube.GetColumnIndex(TradeType);
        var r_pdIx = riskCube.GetColumnIndex("PointDate");

        var cashCube = model.Portfolio.FlowsT0(model.VanillaModel, reportingCcy);
        var cashRows = cashCube.GetAllRows();

        var pvCubeBase = model.PV(reportingCcy);
        var pvRows = pvCubeBase.GetAllRows();
        IPvModel rolledModel = (model is AssetFxMCModel amc) ?
            amc.RollModel(endModel.VanillaModel.BuildDate, currencyProvider, futureSettingsProvider, calendarProvider) :
            (model is AssetFxModel afx ? afx.RollModel(endModel.VanillaModel.BuildDate, currencyProvider) : throw new Exception("Unsupported model type"));
        var newPvCube = rolledModel.PV(reportingCcy);

        var step = newPvCube.QuickDifference(pvCubeBase);
        foreach (var r in step.GetAllRows())
        {
            var row = new Dictionary<string, object>
                {
                    { TradeId, r.MetaData[r_tidIx] },
                    { TradeType, r.MetaData[r_tTypeIx] },
                    { Step, "Theta" },
                    { SubStep, string.Empty },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { "PointDate", endModel.VanillaModel.BuildDate }
                };
            resultsCube.AddRow(row, r.Value);
        }

        //next cash move
        for (var i = 0; i < cashRows.Length; i++)
        {
            var cash = cashRows[i].Value;
            if (cash != 0.0)
            {
                var row = new Dictionary<string, object>
                    {
                        { TradeId,cashRows[i].MetaData[r_tidIx] },
                        { TradeType, cashRows[i].MetaData[r_tTypeIx] },
                        { Step, "Theta" },
                        { SubStep, "CashMove" },
                        { SubSubStep, string.Empty },
                        { PointLabel, string.Empty },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                resultsCube.AddRow(row, cash);
            }
        }

        var currentPvCube = newPvCube;
        //next replace fixings with actual values
        foreach (var fixingDictName in endModel.VanillaModel.FixingDictionaryNames)
        {
            model.VanillaModel.AddFixingDictionary(fixingDictName, endModel.VanillaModel.GetFixingDictionary(fixingDictName));
            model = model.Rebuild(model.VanillaModel, model.Portfolio);
            newPvCube = model.PV(reportingCcy);

            var tidIx = newPvCube.GetColumnIndex(TradeId);
            var tTypeIx = newPvCube.GetColumnIndex(TradeType);

            step = newPvCube.QuickDifference(currentPvCube);
            foreach (var r in step.GetAllRows())
            {
                if (r.Value == 0.0) continue;

                var row = new Dictionary<string, object>
                    {
                        { TradeId, r.MetaData[tidIx] },
                        { TradeType, r.MetaData[tTypeIx] },
                        { Step, "Fixings" },
                        { SubStep, fixingDictName },
                        { SubSubStep, string.Empty },
                        { PointLabel, string.Empty },
                        { "PointDate", endModel.VanillaModel.BuildDate }
                    };
                resultsCube.AddRow(row, r.Value);
            }

            currentPvCube = newPvCube;
        }

        return (currentPvCube, model);
    }
}
