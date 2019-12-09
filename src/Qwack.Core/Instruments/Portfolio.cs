using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments
{
    public class Portfolio : IInstrument
    {
        public List<IInstrument> Instruments { get; set; }
        public string PortfolioName { get; set; }
        public string TradeId => throw new NotImplementedException();

        public string Counterparty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DateTime LastSensitivityDate
        {
            get
            {
                if (Instruments.Count == 0)
                    return DateTime.MinValue;

                var assetTrades = Instruments
                    .Where(x => x is IAssetInstrument);

                var fxTrades = Instruments
                    .Where(x => x is FxForward || (x is CashWrapper cw && cw.UnderlyingInstrument is FxForward));

                if (!fxTrades.Any() && !assetTrades.Any())
                    return DateTime.MinValue;
                else if (!fxTrades.Any())
                    return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate);
                else if (!assetTrades.Any())
                    return fxTrades.Max(x => (x is CashWrapper cw) ? (cw.UnderlyingInstrument as FxForward).DeliveryDate : ((FxForward)x).DeliveryDate);
                return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate).Max(fxTrades.Max(x => (x is CashWrapper cw) ? (cw.UnderlyingInstrument as FxForward).DeliveryDate : ((FxForward)x).DeliveryDate));
            }
        }

        public Currency Currency => throw new NotImplementedException();

        public Portfolio Clone() => new Portfolio
        {
            Instruments = new List<IInstrument>(Instruments),
            PortfolioName = PortfolioName
        };
    }

    public static class PortfolioEx
    {
        public static string[] AssetIds(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return Array.Empty<string>();

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            if(!assetTrades.Any())
                return Array.Empty<string>();

            return assetTrades.SelectMany(x => ((IAssetInstrument)x).AssetIds).Distinct().ToArray();
        }

        public static string[] FxPairs(this Portfolio portfolio, IAssetFxModel model)
        {
            if (portfolio.Instruments.Count == 0)
                return Array.Empty<string>();

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);
            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward || (x is CashWrapper cw && cw.UnderlyingInstrument is FxForward));
            var fxOptionTrades = portfolio.Instruments
                .Where(x => x is FxVanillaOption || (x is CashWrapper cw && cw.UnderlyingInstrument is FxVanillaOption));

            if (!fxTrades.Any() && !assetTrades.Any() && !fxOptionTrades.Any())
                return Array.Empty<string>();

            var o = new List<string>();

            if(fxTrades.Any())
            {
                o.AddRange(fxTrades.Select(x => (x is CashWrapper cw)?(cw.UnderlyingInstrument as FxForward).Pair:((FxForward)x).Pair));
            }
            if (fxOptionTrades.Any())
            {
                o.AddRange(fxOptionTrades.Select(x => (x is CashWrapper cw) ? (cw.UnderlyingInstrument as FxVanillaOption).PairStr : ((FxVanillaOption)x).PairStr));
            }
            if (assetTrades.Any())
            {
                var compoTrades = assetTrades.Select(x=>x as IAssetInstrument)
                    .Where(x => x.FxType(model) != FxConversionType.None);
                if(compoTrades.Any())
                {
                    o.AddRange(compoTrades.Select(x => x.FxPair(model)));
                }
            }

            return o.Distinct().ToArray();
        }

        private static string CpStr(this OptionType callPut) => callPut == OptionType.C ? "Call" : "Put";
        
        public static object[,] Details(this Portfolio pf)
        {
            var nInstrumnets = pf.Instruments.Count;
            var o = new object[nInstrumnets + 1, 11];
            
            //column headers
            o[0, 0] = "TradeId";
            o[0, 1] = "Type";
            o[0, 2] = "Asset";
            o[0, 3] = "Ccy";
            o[0, 4] = "Start";
            o[0, 5] = "End";
            o[0, 6] = "Settle";
            o[0, 7] = "Strike/Rate";
            o[0, 8] = "Quantity";
            o[0, 9] = "Call/Put";
            o[0, 10] = "Counterparty";

            for(var i=0;i<nInstrumnets;i++)
            {
                var ins = pf.Instruments[i];
                if (ins is CashWrapper cw) ins = cw.UnderlyingInstrument;

                o[i + 1, 0] = ins.TradeId ?? string.Empty;
                switch (ins)
                {
                    case AsianOption ao:
                        o[i + 1, 1] = "AsianOption";
                        o[i + 1, 2] = ao.AssetId;
                        o[i + 1, 3] = ao.Currency.Ccy;
                        o[i + 1, 4] = ao.AverageStartDate;
                        o[i + 1, 5] = ao.AverageEndDate;
                        o[i + 1, 6] = ao.PaymentDate;
                        o[i + 1, 7] = ao.Strike;
                        o[i + 1, 8] = ao.Notional;
                        o[i + 1, 9] = ao.CallPut.CpStr();
                        o[i + 1, 10] = ao.Counterparty ?? string.Empty;
                        break;
                    case AsianSwap asw:
                        o[i + 1, 1] = "AsianSwap";
                        o[i + 1, 2] = asw.AssetId;
                        o[i + 1, 3] = asw.Currency.Ccy;
                        o[i + 1, 4] = asw.AverageStartDate;
                        o[i + 1, 5] = asw.AverageEndDate;
                        o[i + 1, 6] = asw.PaymentDate;
                        o[i + 1, 7] = asw.Strike;
                        o[i + 1, 8] = asw.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = asw.Counterparty ?? string.Empty;
                        break;
                    case AsianSwapStrip asws:
                        o[i + 1, 1] = "AsianSwapStrip";
                        o[i + 1, 2] = asws.Swaplets.First().AssetId;
                        o[i + 1, 3] = asws.Currency.Ccy;
                        o[i + 1, 4] = asws.Swaplets.First().AverageStartDate;
                        o[i + 1, 5] = asws.Swaplets.Last().AverageEndDate;
                        o[i + 1, 6] = asws.Swaplets.Last().PaymentDate;
                        o[i + 1, 7] = asws.Swaplets.First().Strike;
                        o[i + 1, 8] = asws.Swaplets.Sum(x => x.Notional);
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = asws.Counterparty??string.Empty;
                        break;
                    case EuropeanOption eo:
                        o[i + 1, 1] = "EuroOption";
                        o[i + 1, 2] = eo.AssetId;
                        o[i + 1, 3] = eo.Currency.Ccy;
                        o[i + 1, 4] = eo.ExpiryDate;
                        o[i + 1, 5] = eo.ExpiryDate;
                        o[i + 1, 6] = eo.PaymentDate;
                        o[i + 1, 7] = eo.Strike;
                        o[i + 1, 8] = eo.Notional;
                        o[i + 1, 9] = eo.CallPut.CpStr(); 
                        o[i + 1, 10] = eo.Counterparty ?? string.Empty;
                        break;
                    case FuturesOption fo:
                        o[i + 1, 1] = "FuturesOption";
                        o[i + 1, 2] = fo.AssetId;
                        o[i + 1, 3] = fo.Currency.Ccy;
                        o[i + 1, 4] = fo.ExpiryDate;
                        o[i + 1, 5] = fo.ExpiryDate;
                        o[i + 1, 6] = fo.ExpiryDate;
                        o[i + 1, 7] = fo.Strike;
                        o[i + 1, 8] = fo.ContractQuantity * fo.LotSize;
                        o[i + 1, 9] = fo.CallPut.CpStr();
                        o[i + 1, 10] = fo.Counterparty ?? string.Empty;
                        break;
                    case Forward f:
                        o[i + 1, 1] = "Forward";
                        o[i + 1, 2] = f.AssetId;
                        o[i + 1, 3] = f.Currency.Ccy;
                        o[i + 1, 4] = f.ExpiryDate;
                        o[i + 1, 5] = f.ExpiryDate;
                        o[i + 1, 6] = f.PaymentDate;
                        o[i + 1, 7] = f.Strike;
                        o[i + 1, 8] = f.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = f.Counterparty ?? string.Empty;
                        break;
                    case Future fut:
                        o[i + 1, 1] = "Future";
                        o[i + 1, 2] = fut.AssetId;
                        o[i + 1, 3] = fut.Currency.Ccy;
                        o[i + 1, 4] = fut.ExpiryDate;
                        o[i + 1, 5] = fut.ExpiryDate;
                        o[i + 1, 6] = fut.ExpiryDate;
                        o[i + 1, 7] = fut.Strike;
                        o[i + 1, 8] = fut.ContractQuantity;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = fut.Counterparty ?? string.Empty;
                        break;
                    case FxForward fx:
                        o[i + 1, 1] = "FxForward";
                        o[i + 1, 2] = fx.Pair;
                        o[i + 1, 3] = fx.DomesticCCY.Ccy;
                        o[i + 1, 4] = fx.DeliveryDate;
                        o[i + 1, 5] = fx.DeliveryDate;
                        o[i + 1, 6] = fx.DeliveryDate;
                        o[i + 1, 7] = fx.Strike;
                        o[i + 1, 8] = fx.DomesticQuantity;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = fx.Counterparty ?? string.Empty;
                        break;
                    case FixedRateLoanDeposit l:
                        o[i + 1, 1] = "LoanDepo";
                        o[i + 1, 2] = l.Currency.Ccy;
                        o[i + 1, 3] = l.Currency.Ccy;
                        o[i + 1, 4] = l.StartDate;
                        o[i + 1, 5] = l.EndDate;
                        o[i + 1, 6] = l.EndDate;
                        o[i + 1, 7] = l.InterestRate;
                        o[i + 1, 8] = l.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = l.Counterparty ?? string.Empty;
                        break;
                    case FloatingRateLoanDepo lf:
                        o[i + 1, 1] = "LoanDepoFloat";
                        o[i + 1, 2] = lf.Currency.Ccy;
                        o[i + 1, 3] = lf.Currency.Ccy;
                        o[i + 1, 4] = lf.LoanDepoSchedule.Flows.Where(x=>x.FlowType==FlowType.FloatRate).Min(x=>x.AccrualPeriodStart);
                        o[i + 1, 5] = lf.LastSensitivityDate;
                        o[i + 1, 6] = lf.LastSensitivityDate;
                        o[i + 1, 7] = lf.LoanDepoSchedule.Flows.Where(x => x.FlowType == FlowType.FloatRate).Average(x => x.FixedRateOrMargin);
                        o[i + 1, 8] = lf.LoanDepoSchedule.Flows.Where(x => x.FlowType == FlowType.FloatRate).Average(x => x.Notional);
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = lf.Counterparty ?? string.Empty;
                        break;
                    case PhysicalBalance pb:
                        o[i + 1, 1] = "Physical";
                        o[i + 1, 2] = pb.Currency.Ccy;
                        o[i + 1, 3] = pb.Currency.Ccy;
                        o[i + 1, 4] = pb.PayDate == DateTime.MinValue ? string.Empty : (object)pb.PayDate;
                        o[i + 1, 5] = pb.PayDate == DateTime.MinValue ? string.Empty : (object)pb.PayDate;
                        o[i + 1, 6] = pb.PayDate == DateTime.MinValue ? string.Empty : (object)pb.PayDate;
                        o[i + 1, 7] = string.Empty;
                        o[i + 1, 8] = pb.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = pb.Counterparty ?? string.Empty;
                        break;
                    case CashBalance c:
                        o[i + 1, 1] = "Cash";
                        o[i + 1, 2] = c.Currency.Ccy;
                        o[i + 1, 3] = c.Currency.Ccy;
                        o[i + 1, 4] = c.PayDate == DateTime.MinValue ? string.Empty : (object)c.PayDate;
                        o[i + 1, 5] = c.PayDate == DateTime.MinValue ? string.Empty : (object)c.PayDate;
                        o[i + 1, 6] = c.PayDate == DateTime.MinValue ? string.Empty : (object)c.PayDate;
                        o[i + 1, 7] = string.Empty;
                        o[i + 1, 8] = c.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = c.Counterparty ?? string.Empty;
                        break;
                    case ETC etc:
                        o[i + 1, 1] = "ETC";
                        o[i + 1, 2] = etc.AssetId;
                        o[i + 1, 3] = etc.Currency.Ccy;
                        o[i + 1, 4] = etc.LastSensitivityDate;
                        o[i + 1, 5] = etc.LastSensitivityDate;
                        o[i + 1, 6] = etc.LastSensitivityDate;
                        o[i + 1, 7] = string.Empty;
                        o[i + 1, 8] = etc.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = etc.Counterparty ?? string.Empty;
                        break;
                }
            }

            return o;
        }

        public static (Portfolio newTrades, Portfolio removedTrades, Portfolio ammendedTradesStart, Portfolio ammendedTradesEnd) ActivityBooks(this Portfolio start, Portfolio end, DateTime endDate)
        {
            var startIds = start.Instruments.Select(x => x.TradeId).ToList();
            var newTradesIns = end.Instruments.Where(i => !startIds.Contains(i.TradeId));
            var newTrades = new Portfolio { Instruments = newTradesIns.ToList() };

            var endIds = end.Instruments.Select(x => x.TradeId).ToList();
            var removedTradesIns = start.Instruments
                .Where(i => 
                !endIds.Contains(i.TradeId) && 
                ((!(i is AsianSwap asw) && i.LastSensitivityDate > endDate )|| (i is CashBalance cb && cb.PayDate != endDate) || (i is AsianSwap aswp && aswp.PaymentDate > endDate)));
            var removedTrades = new Portfolio { Instruments = removedTradesIns.ToList() };

            var commonIds = startIds.Intersect(endIds).ToList();

            var ammendedTradesStart = new Portfolio { Instruments = new List<IInstrument>() };
            var ammendedTradesEnd = new Portfolio { Instruments = new List<IInstrument>() };

            foreach(var id in commonIds)
            {
                var startIns = start.Instruments.Where(x => x.TradeId == id).First();
                var endIns = end.Instruments.Where(x => x.TradeId == id).First();
                if (!startIns.Equals(endIns) && !(startIns is CashBalance))
                {
                    ammendedTradesStart.Instruments.Add(startIns);
                    ammendedTradesEnd.Instruments.Add(endIns);
                }
            }

            return (newTrades, removedTrades, ammendedTradesStart, ammendedTradesEnd);
        }

        public static bool Equals(this IInstrument A, IInstrument B)
        {
            switch(A)
            {
                case AsianOption asianOption:
                    return asianOption.Equals((AsianOption)B);
                case AsianSwap asianSwap:
                    return asianSwap.Equals((AsianSwap)B);
                case AsianSwapStrip asianSwapStrip:
                    return asianSwapStrip.Equals((AsianSwapStrip)B);
                case AsianBasisSwap asianBasisSwap:
                    return asianBasisSwap.Equals((AsianBasisSwap)B);
                case FxForward fxForward:
                    return fxForward.Equals((FxForward)B);
                case EuropeanOption europeanOption:
                    return europeanOption.Equals((EuropeanOption)B);
                case Forward forward:
                    return forward.Equals((Forward)B);
                case FuturesOption option:
                    return option.Equals((FuturesOption)B);
                case Future future:
                    return future.Equals((Future)B);
                case CashBalance cash:
                    return cash.Equals((CashBalance)B);
                case FixedRateLoanDeposit loanDeposit:
                    return loanDeposit.Equals((FixedRateLoanDeposit)B);
                case FloatingRateLoanDepo loanDepositFl:
                    return loanDepositFl.Equals((FloatingRateLoanDepo)B);
                case CashWrapper wrapper:
                    return Equals(wrapper.UnderlyingInstrument, (CashWrapper)B);
                case ETC etc:
                    return etc.Equals((ETC)B);
                default:
                    return false;
            }
        }

        private static double SupFacByHedgeSet(string hedgeSet) => hedgeSet == "Electricity" ? 0.4 : 0.18;

        public static double SaCcrAddon(this Portfolio pf, IAssetFxModel model, Currency reportingCcy, Dictionary<string, string> AssetIdToHedgingSetMap)
        {
            if (!pf.Instruments.All(x => x is ISaCcrEnabled))
                throw new Exception("Portfolio contains non-SACCR enabled instruments");

            var assetIds = pf.AssetIds();
            var missingMappings = assetIds.Where(x => !AssetIdToHedgingSetMap.ContainsKey(x));
            if (missingMappings.Any())
                throw new Exception($"AssetId-to-set mapping missing for {missingMappings.First()}");

            var addOnByAsset = new Dictionary<string, double>();
            var insByAssetId = pf.Instruments.Select(x => x as IAssetInstrument).GroupBy(x => x.AssetIds.First());
            foreach (var insGroup in insByAssetId)
            {
                addOnByAsset.Add(insGroup.Key, 0.0);
                foreach (ISaCcrEnabled ins in insGroup)
                {
                    var aIns = (IAssetInstrument)ins;
                    ins.HedgingSet = AssetIdToHedgingSetMap[aIns.AssetIds.First()];
                    var fxRate = aIns.Currency == reportingCcy ? 1.0 : model.FundingModel.GetFxRate(model.BuildDate, aIns.Currency, reportingCcy);
                    addOnByAsset[insGroup.Key] += ins.EffectiveNotional(model) * SupFacByHedgeSet(ins.HedgingSet) * fxRate;
                }
            }


            var activeHedgeSetNames = addOnByAsset.Select(x => AssetIdToHedgingSetMap[x.Key]).Distinct();
            var activeHedgeSets = activeHedgeSetNames.ToDictionary(x => x, x => addOnByAsset.Where(y => AssetIdToHedgingSetMap[y.Key] == x));
            var addOnByHedgeSet = new Dictionary<string, double>();
            var correl = 0.4;
            foreach(var hs in activeHedgeSets)
            {
                addOnByHedgeSet[hs.Key] = 0.0;
                var term1 = System.Math.Pow(correl * hs.Value.Sum(x => x.Value), 2.0);
                var term2 = (1.0-correl*correl) * hs.Value.Sum(x => x.Value* x.Value);
                addOnByHedgeSet[hs.Key] = System.Math.Sqrt(term1 + term2);
            }

            var addOn = addOnByHedgeSet.Sum(x=>x.Value);
            
            return addOn;
        }

        public static double SaCcrEAD(this Portfolio pf, IPvModel model, Currency reportingCcy, Dictionary<string, string> AssetIdToHedgingSetMap)
        {
            var pfe = SaCcrAddon(pf, model.VanillaModel, reportingCcy, AssetIdToHedgingSetMap);
            var pvModel = model.Rebuild(model.VanillaModel, pf);
            var rcCube = pvModel.PV(reportingCcy);
            var rc = System.Math.Max(0.0, rcCube.GetAllRows().Sum(x => x.Value));
            var alpha = 1.4;

            return alpha * (rc + pfe);
        }

        public static Portfolio RollWithLifecycle(this Portfolio pf, DateTime rollfromDate, DateTime rollToDate, bool cashOnDayAlreadyPaid=false)
        {
            var o = new Portfolio
            {
                PortfolioName = pf.PortfolioName,
                Instruments = new List<IInstrument>()
            };
            foreach(var i in pf.Instruments)
            {
                switch(i)
                {
                    case FxForward fxf:
                        if(fxf.DeliveryDate<rollToDate && fxf.DeliveryDate >= rollfromDate)
                        {
                            o.Instruments.Add(new CashBalance(fxf.DomesticCCY, fxf.DomesticQuantity) { TradeId = i.TradeId + "d" });
                            o.Instruments.Add(new CashBalance(fxf.ForeignCCY, -fxf.DomesticQuantity * fxf.Strike) { TradeId = i.TradeId + "f" });
                        }
                        else
                        {
                            o.Instruments.Add(fxf);
                        }
                        break;
                    case FixedRateLoanDeposit l:
                        if (l.EndDate < rollToDate && l.EndDate >= rollfromDate)
                        {
                            o.Instruments.Add(new CashBalance(l.Currency, -l.Notional) { TradeId = i.TradeId + "n" });
                        }
                        else
                        {
                            o.Instruments.Add(l);
                        }
                        break;
                    case CashWrapper w:
                        var wrapper = (CashWrapper)w.Clone();
                        switch (wrapper.UnderlyingInstrument)
                        {
                            case FxForward wfxf:
                                if (wfxf.DeliveryDate < rollToDate && wfxf.DeliveryDate >= rollfromDate)
                                {
                                    wrapper.CashBalances.Add(new CashBalance(wfxf.DomesticCCY, wfxf.DomesticQuantity) { TradeId = i.TradeId + "d" });
                                    wrapper.CashBalances.Add(new CashBalance(wfxf.ForeignCCY, -wfxf.DomesticQuantity * wfxf.Strike) { TradeId = i.TradeId + "f" });
                                }                                break;
                            case FixedRateLoanDeposit wl:
                                if (wl.StartDate < rollToDate && ((!cashOnDayAlreadyPaid && wl.StartDate >= rollfromDate) || (cashOnDayAlreadyPaid && wl.StartDate > rollfromDate)))
                                {
                                    wrapper.CashBalances.Add(new CashBalance(wl.Currency, wl.Notional) { TradeId = i.TradeId + "nStart" });
                                }
                                if (wl.EndDate < rollToDate && ((!cashOnDayAlreadyPaid && wl.EndDate >= rollfromDate) || (cashOnDayAlreadyPaid && wl.EndDate > rollfromDate)))
                                {
                                    wrapper.CashBalances.Add(new CashBalance(wl.Currency, -wl.Notional) { TradeId = i.TradeId + "n" });
                                }
                                break;
                        }
                        o.Instruments.Add(wrapper);
                        break;
                    default:
                        o.Instruments.Add(i);
                        break;

                }
            }

            return o;
        }

        public static Portfolio CashAccrual(this Portfolio pf, DateTime rollToDate, IFundingModel model)
        {
            var o = new Portfolio
            {
                PortfolioName = pf.PortfolioName,
                Instruments = new List<IInstrument>()
            };
            foreach (var i in pf.Instruments)
            {
                switch (i)
                {
                    case CashBalance cash:
                        o.Instruments.Add(cash.PayDate > rollToDate ?
                            cash :
                            new CashBalance(cash.Currency, cash.Notional / model.GetDf(cash.Currency, model.BuildDate, rollToDate))
                            { TradeId = cash.TradeId, Counterparty = cash.Counterparty, PortfolioName = cash.PortfolioName });
                        break;
                    default:
                        o.Instruments.Add(i);
                        break;
                }
            }

            return o;
        }

        public static Portfolio FilterOnSettleDate(this Portfolio pf, DateTime filterOutOnOrBefore) 
            => new Portfolio { Instruments = pf.Instruments.Where(x => x.LastSensitivityDate > filterOutOnOrBefore || (x is CashBalance)).ToList() };
    }
}
