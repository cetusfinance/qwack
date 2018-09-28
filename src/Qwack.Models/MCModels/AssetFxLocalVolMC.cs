using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;
using Qwack.Core.Instruments;
using Qwack.Math.Extensions;
using Qwack.Paths;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Paths.Processes;
using Qwack.Dates;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;

namespace Qwack.Models.MCModels
{
    public class AssetFxLocalVolMC : IMcModel
    {
        public PathEngine Engine { get; }
        public Portfolio Portfolio { get; }
        public IAssetFxModel Model { get; }
        private readonly Dictionary<string, AssetPathPayoff> _payoffs;

        public AssetFxLocalVolMC(DateTime originDate, Portfolio portfolio, IAssetFxModel model, McSettings settings)
        {
            Engine = new PathEngine(settings.NumberOfPaths);
            Portfolio = portfolio;
            Model = model;

            switch(settings.Generator)
            {
                case RandomGeneratorType.MersenneTwister:
                    Engine.AddPathProcess(new Random.MersenneTwister.MersenneTwister64()
                    {
                        UseNormalInverse = true,
                        UseAnthithetic = false
                    });
                    break;
                case RandomGeneratorType.Sobol:
                    var directionNumers = new Random.Sobol.SobolDirectionNumbers("SobolDirectionNumbers.txt");
                    Engine.AddPathProcess(new Random.Sobol.SobolPathGenerator(directionNumers, 1000)
                    {
                        UseNormalInverse = true
                    });
                    break;
            }

            var lastDate = portfolio.LastSensitivityDate();
            var assetIds = portfolio.AssetIds();
            var assetInstruments = portfolio.Instruments
               .Where(x => x is IAssetInstrument)
               .Select(x => x as IAssetInstrument);

            var fixingsNeeded = new Dictionary<string, List<DateTime>>();
            foreach (var ins in assetInstruments)
            {
                var fixingsForIns = ins.PastFixingDates(originDate);
                if (fixingsForIns.Any())
                {
                    foreach (var kv in fixingsForIns)
                    {
                        if (!fixingsNeeded.ContainsKey(kv.Key))
                            fixingsNeeded.Add(kv.Key, new List<DateTime>());

                        fixingsNeeded[kv.Key] = fixingsNeeded[kv.Key].Concat(kv.Value).Distinct().ToList();
                    }
                }
            }

            foreach (var assetId in assetIds)
            {
                var surface = model.GetVolSurface(assetId);
                var fwdCurve = new Func<double, double>(t => 
                {
                    return model
                    .GetPriceCurve(assetId)
                    .GetPriceForDate(originDate.AddYearFraction(t, DayCountBasis.ACT365F));
                });

                var fixingDict = fixingsNeeded.ContainsKey(assetId) ? model.GetFixingDictionary(assetId) : null;
                var fixings = fixingDict!=null ?
                    fixingsNeeded[assetId].ToDictionary(x => x, x => fixingDict[x])
                    : new Dictionary<DateTime, double>();

                var asset = new LVSingleAsset
                (
                    startDate: originDate,
                    expiryDate: lastDate,
                    volSurface: surface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: settings.NumberOfTimesteps,
                    name: assetId,
                    pastFixings: fixings
                );
                Engine.AddPathProcess(asset);
            }

            var fxPairs = portfolio.FxPairs(model);
            foreach (var fxPair in fxPairs)
            {
                var pair = fxPair.FxPairFromString();
                string fxPairName;
                if (!model.FundingModel.VolSurfaces.ContainsKey(fxPair))
                {
                    var flippedPair = $"{pair.Foreign.Ccy}/{pair.Domestic.Ccy}";
                    if (!model.FundingModel.VolSurfaces.ContainsKey(fxPair))
                    {
                        throw new Exception($"Could not find Fx vol surface for {fxPair} or {flippedPair}");
                    }

                    pair = flippedPair.FxPairFromString();
                    fxPairName = flippedPair;
                }
                else
                {
                    fxPairName = fxPair;
                }

                var surface = model.FundingModel.VolSurfaces[fxPairName];

                var fwdCurve = new Func<double, double>(t =>
                {
                    var date = originDate.AddYearFraction(t, DayCountBasis.ACT365F);
                    return model.FundingModel.GetFxRate(date, fxPairName);
                });
                var asset = new LVSingleAsset
                (
                    startDate: originDate,
                    expiryDate: lastDate,
                    volSurface: surface,
                    forwardCurve: fwdCurve,
                    nTimeSteps: settings.NumberOfTimesteps,
                    name: fxPairName
                );
                Engine.AddPathProcess(asset);
            }

           

            _payoffs = assetInstruments.ToDictionary(x => x.TradeId, y => new AssetPathPayoff(y));
            foreach (var product in _payoffs)
            {
                Engine.AddPathProcess(product.Value);
            }

            Engine.SetupFeatures();

        }

        public ICube PV(Currency reportingCurrency)
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "TradeId", typeof(string) },
                { "Currency", typeof(string) }
            };
            cube.Initialize(dataTypes);

            Engine.RunProcess();

            foreach (var kv in _payoffs)
            {
                var pv = 0.0;
                var fxRate = 1.0;
                string tradeId = null;
                var ccy = reportingCurrency?.ToString();
                var insQuery = Portfolio.Instruments.Where(x => x.TradeId == kv.Key);
                if (!insQuery.Any())
                    throw new Exception($"Trade with id {kv.Key} not found");
                if(insQuery.Count()!=1)
                    throw new Exception($"Trade with id {kv.Key} has multiple results");

                var ins = insQuery.First();
                switch (ins)
                {
                    case AsianSwap swap:
                        pv = kv.Value.AverageResult;
                        tradeId = swap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, swap.PaymentCurrency);
                        else
                            ccy = swap.PaymentCurrency.ToString();
                        break;
                    case AsianSwapStrip swapStrip:
                        pv = kv.Value.AverageResult;
                        tradeId = swapStrip.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, swapStrip.Swaplets.First().PaymentCurrency);
                        else
                            ccy = swapStrip.Swaplets.First().PaymentCurrency.ToString();
                        break;
                    case AsianBasisSwap basisSwap:
                        pv = kv.Value.AverageResult;
                        tradeId = basisSwap.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, basisSwap.PaySwaplets.First().PaymentCurrency);
                        else
                            ccy = basisSwap.PaySwaplets.First().PaymentCurrency.ToString();
                        break;
                    case Forward fwd:
                        pv = kv.Value.AverageResult;
                        tradeId = fwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, fwd.PaymentCurrency);
                        else
                            ccy = fwd.PaymentCurrency.ToString();
                        break;
                    case Future fut:
                        pv = kv.Value.AverageResult;
                        tradeId = fut.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, fut.Currency);
                        else
                            ccy = fut.Currency.ToString();
                        break;
                    case FxForward fxFwd:
                        pv = kv.Value.AverageResult;
                        tradeId = fxFwd.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, fxFwd.ForeignCCY);
                        else
                            ccy = fxFwd.ForeignCCY.ToString();
                        break;
                    case FixedRateLoanDeposit loanDepo:
                        pv = kv.Value.AverageResult;
                        tradeId = loanDepo.TradeId;
                        if (reportingCurrency != null)
                            fxRate = Model.FundingModel.GetFxRate(Model.BuildDate, reportingCurrency, loanDepo.Ccy);
                        else
                            ccy = loanDepo.Ccy.ToString();
                        break;
                    default:
                        throw new Exception($"Unabled to handle product of type {ins.GetType()}");
                }


                var row = new Dictionary<string, object>
                {
                    { "TradeId", tradeId },
                    { "Currency", ccy }
                };
                cube.AddRow(row, pv / fxRate);
            }
            return cube;
        }

    }
}
