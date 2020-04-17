using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Models.Models;

namespace Qwack.Models.Solvers
{
    public static class SimplePortfolioSolver
    {
        public static double SolveStrikeForPV(this Portfolio portfolio, IAssetFxModel model, double targetPV)
        {
            var insList = portfolio.Instruments.Select(x => x as IAssetInstrument).ToList();

            if (insList.Any(x => x == null))
                throw new Exception("Not all instruments in the portfolio implement IAssetInstrument");

            var targetFunc = new Func<double, double>(k =>
             {
                 var newPf = new Portfolio()
                 {
                     Instruments = insList.Select(i =>(IInstrument)i.SetStrike(k)).ToList()
                 };
                 var pv = newPf.PV(model).GetAllRows().Sum(x => x.Value);
                 return pv - targetPV;
             });
       
            var firstGuess = insList.Average(i=>i.ParRate(model));

            var solvedStrike = Math.Solvers.Newton1D.MethodSolve(targetFunc, firstGuess, 1e-8, 1000, 1e-9);

            if (System.Math.Abs(targetFunc(solvedStrike)) < 1e-8)
                return solvedStrike;
            else
                throw new Exception("Failed to find solution after 1000 itterations");
        }

        public static double SolveStrikeForGrossRoC(this Portfolio portfolio, IAssetFxModel model, double targetRoC, Currency reportingCurrency, 
            HazzardCurve hazzardCurve, double LGD, double xVA_LGD, double partyRiskWeight, double cvaCapitalWeight, IIrCurve discountCurve, 
            ICurrencyProvider currencyProvider, Dictionary<string, string> assetIdToHedgeMap, Dictionary<string, double> hedgeGroupCCFs)
        {
            var insList = portfolio.Instruments.Select(x => x as IAssetInstrument).ToList();

            if (insList.Any(x => x == null))
                throw new Exception("Not all instruments in the portfolio implement IAssetInstrument");

            var rolledModels = new Dictionary<DateTime, IAssetFxModel>();
            var d = model.BuildDate;
            var lastModel = model;
            while (d <= portfolio.LastSensitivityDate)
            {
                rolledModels.Add(d, lastModel);
                d = d.AddDays(1);
                lastModel = lastModel.RollModel(d, currencyProvider);
            }

            var targetFunc = new Func<double, double>(k =>
            {
                var newPf = new Portfolio()
                {
                    Instruments = insList.Select(i => (IInstrument)i.SetStrike(k)).ToList()
                };
                var roc = newPf.GrossRoC(model, reportingCurrency, hazzardCurve, LGD, xVA_LGD, cvaCapitalWeight, partyRiskWeight, discountCurve, currencyProvider, rolledModels, assetIdToHedgeMap, hedgeGroupCCFs);
                return roc-targetRoC;
            });

            var firstGuess = insList.Average(i => i.ParRate(model));

            var solvedStrike = Math.Solvers.Newton1D.MethodSolve(targetFunc, firstGuess, 1e-8, 1000, 1e-9);

            if (System.Math.Abs(targetFunc(solvedStrike)) < 1e-8)
                return solvedStrike;
            else
                throw new Exception("Failed to find solution after 1000 itterations");
        }
    }
}
