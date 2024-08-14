using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Models.AttributionSteps;

public class ActivityStep(Portfolio startPortfolio, Portfolio endPortfolio) : IPnLAttributionStep
{
    public (ICube endOfStepPvCube, IPvModel model) Attribute(IPvModel model, IPvModel endModel, ResultCube resultsCube, ICube lastPvCube,
        ICube riskCube, Currency reportingCcy)
    {
        var (newTrades, removedTrades, ammendedTradesStart, ammendedTradesEnd) = startPortfolio.ActivityBooks(endPortfolio, endModel.VanillaModel.BuildDate);

        var pfEndDict = endPortfolio.Instruments.ToDictionary(x => x.TradeId, x => x.PortfolioName);
        var pfStartDict = startPortfolio.Instruments.ToDictionary(x => x.TradeId, x => x.PortfolioName);

        if (newTrades.Instruments.Count > 0)
        {
            model = model.Rebuild(model.VanillaModel, newTrades);
            var newTradesPnL = model.PV(reportingCcy);
            var tidIx = newTradesPnL.GetColumnIndex(TradeId);
            var tTypeIx = newTradesPnL.GetColumnIndex(TradeType);
            var r_UlIx = newTradesPnL.GetColumnIndex(Underlying);
            foreach (var t in newTradesPnL.GetAllRows())
            {
                var tid = (string)t.MetaData[tidIx];
                var row = new Dictionary<string, object>
                {
                    { TradeId,  tid},
                    { TradeType, t.MetaData[tTypeIx] },
                    { Step, "Activity" },
                    { SubStep, "New" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { "Portfolio", pfEndDict[tid]},
                    { Underlying, r_UlIx<0 ? string.Empty : t.MetaData[r_UlIx] }
                };
                resultsCube.AddRow(row, t.Value);
            }
        }

        if (removedTrades.Instruments.Count > 0)
        {
            model = model.Rebuild(model.VanillaModel, removedTrades);
            var removedTradesPnL = model.PV(reportingCcy);
            
            var tidIx = removedTradesPnL.GetColumnIndex(TradeId);
            var tTypeIx = removedTradesPnL.GetColumnIndex(TradeType);
            var r_UlIx = removedTradesPnL.GetColumnIndex(Underlying);
            foreach (var t in removedTradesPnL.GetAllRows())
            {
                var tid = (string)t.MetaData[tidIx];
                var row = new Dictionary<string, object>
                {
                    { TradeId, tid },
                    { TradeType, t.MetaData[tTypeIx] },
                    { Step, "Activity" },
                    { SubStep, "Removed" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { "Portfolio", pfStartDict[tid]},
                    { Underlying, r_UlIx<0 ? string.Empty : t.MetaData[r_UlIx] }
                };
                resultsCube.AddRow(row, -t.Value);
            }
        }

        if (ammendedTradesStart.Instruments.Count > 0)
        {
            model = model.Rebuild(model.VanillaModel, ammendedTradesStart);
            var amendedTradesPnLStart = model.PV(reportingCcy);
            model = model.Rebuild(model.VanillaModel, ammendedTradesEnd);
            var amendedTradesPnLEnd = model.PV(reportingCcy);
            var amendedPnL = amendedTradesPnLEnd.QuickDifference(amendedTradesPnLStart);
            
            var tidIx = amendedTradesPnLStart.GetColumnIndex(TradeId);
            var tTypeIx = amendedTradesPnLStart.GetColumnIndex(TradeType);
            var r_UlIx = amendedTradesPnLStart.GetColumnIndex(Underlying);

            foreach (var t in amendedPnL.GetAllRows())
            {
                var tid = (string)t.MetaData[tidIx];
                var row = new Dictionary<string, object>
                {
                    { TradeId, tid },
                    { TradeType, t.MetaData[tTypeIx] },
                    { Step, "Activity" },
                    { SubStep, "Ammended" },
                    { SubSubStep, string.Empty },
                    { PointLabel, string.Empty },
                    { "Portfolio", pfStartDict[tid]},
                    { Underlying, r_UlIx<0 ? string.Empty : t.MetaData[r_UlIx] }
                };
                resultsCube.AddRow(row, t.Value);
            }
        }

        model = model.Rebuild(model.VanillaModel, endPortfolio);

        return (lastPvCube, model);
    }
}
