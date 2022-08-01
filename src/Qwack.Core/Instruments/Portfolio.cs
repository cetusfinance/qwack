using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using static System.Math;

namespace Qwack.Core.Instruments
{
    public class Portfolio : IInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
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

        public Portfolio Clone() => new()
        {
            Instruments = new List<IInstrument>(Instruments),
            PortfolioName = PortfolioName
        };

        public TO_Portfolio ToTransportObject() =>
            new()
            {
                PortfolioName = PortfolioName,
                Instruments = Instruments.Select(x => x.GetTransportObject()).ToList()
            };
    }

    public static class PortfolioEx
    {
        public static string[] AssetIds(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            if (!assetTrades.Any())
                return new string[0];

            return assetTrades.SelectMany(x => ((IAssetInstrument)x).AssetIds).Distinct().ToArray();
        }

        public static string[] AssetIdsWithVega(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IHasVega);

            if (!assetTrades.Any())
                return new string[0];

            return assetTrades.SelectMany(x => ((IAssetInstrument)x).AssetIds).Distinct().ToArray();
        }

        public static bool IsFx(string name) => name.Length == 7 && name[3] == '/';

        public static string[] FxPairs(this Portfolio portfolio, IAssetFxModel model)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);
            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward || (x is CashWrapper cw && cw.UnderlyingInstrument is FxForward));
            var fxOptionTrades = portfolio.Instruments
                .Where(x => x is FxVanillaOption || (x is CashWrapper cw && cw.UnderlyingInstrument is FxVanillaOption));

            if (!fxTrades.Any() && !assetTrades.Any() && !fxOptionTrades.Any())
                return new string[0];

            var o = new List<string>();

            if (fxTrades.Any())
            {
                o.AddRange(fxTrades.Select(x => (x is CashWrapper cw) ? (cw.UnderlyingInstrument as FxForward).Pair : ((FxForward)x).Pair));
            }
            if (fxOptionTrades.Any())
            {
                o.AddRange(fxOptionTrades.Select(x => (x is CashWrapper cw) ? (cw.UnderlyingInstrument as FxVanillaOption).Pair : ((FxVanillaOption)x).Pair));
            }
            if (assetTrades.Any())
            {
                var compoTrades = assetTrades.Select(x => x as IAssetInstrument)
                    .Where(x => x.FxType(model) != FxConversionType.None);
                var assetIsFxTrades = assetTrades.Select(x => x as IAssetInstrument)
                    .Where(x => IsFx(x.AssetIds.First()));
                if (compoTrades.Any())
                {
                    o.AddRange(compoTrades.Select(x => x.FxPair(model)));
                }
                if (assetIsFxTrades.Any())
                {
                    o.AddRange(assetIsFxTrades.Select(x => x.AssetIds.First()));
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

            for (var i = 0; i < nInstrumnets; i++)
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
                        o[i + 1, 10] = asws.Counterparty ?? string.Empty;
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
                        o[i + 1, 4] = lf.LoanDepoSchedule.Flows.Min(x => x.AccrualPeriodStart);
                        o[i + 1, 5] = lf.LastSensitivityDate;
                        o[i + 1, 6] = lf.LastSensitivityDate;
                        o[i + 1, 7] = lf.LoanDepoSchedule.Flows.Average(x => x.FixedRateOrMargin);
                        o[i + 1, 8] = lf.LoanDepoSchedule.Flows.Average(x => x.Notional);
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
                ((i is not AsianSwap asw && i.LastSensitivityDate > endDate) || (i is CashBalance cb && cb.PayDate != endDate) || (i is AsianSwap aswp && aswp.PaymentDate > endDate)));
            var removedTrades = new Portfolio { Instruments = removedTradesIns.ToList() };

            var commonIds = startIds.Intersect(endIds).ToList();

            var ammendedTradesStart = new Portfolio { Instruments = new List<IInstrument>() };
            var ammendedTradesEnd = new Portfolio { Instruments = new List<IInstrument>() };

            foreach (var id in commonIds)
            {
                var startIns = start.Instruments.Where(x => x.TradeId == id).First();
                var endIns = end.Instruments.Where(x => x.TradeId == id).First();
                if (!startIns.Equals(endIns) && startIns is not CashBalance)
                {
                    ammendedTradesStart.Instruments.Add(startIns);
                    ammendedTradesEnd.Instruments.Add(endIns);
                }
            }

            return (newTrades, removedTrades, ammendedTradesStart, ammendedTradesEnd);
        }

        public static bool Equals(this IInstrument A, IInstrument B) => A switch
        {
            AsianOption asianOption => asianOption.Equals((AsianOption)B),
            AsianSwap asianSwap => asianSwap.Equals((AsianSwap)B),
            AsianSwapStrip asianSwapStrip => asianSwapStrip.Equals((AsianSwapStrip)B),
            AsianBasisSwap asianBasisSwap => asianBasisSwap.Equals((AsianBasisSwap)B),
            FxForward fxForward => fxForward.Equals((FxForward)B),
            EuropeanOption europeanOption => europeanOption.Equals((EuropeanOption)B),
            Forward forward => forward.Equals((Forward)B),
            FuturesOption option => option.Equals((FuturesOption)B),
            Future future => future.Equals((Future)B),
            CashBalance cash => cash.Equals((CashBalance)B),
            FixedRateLoanDeposit loanDeposit => loanDeposit.Equals((FixedRateLoanDeposit)B),
            FloatingRateLoanDepo loanDepositFl => loanDepositFl.Equals((FloatingRateLoanDepo)B),
            CashWrapper wrapper => Equals(wrapper.UnderlyingInstrument, (CashWrapper)B),
            CashAsset etc => etc.Equals((CashAsset)B),
            _ => false,
        };

        public static double Notional(this IInstrument ins) => ins switch
        {
            AsianOption asianOption => asianOption.Notional,
            AsianSwap asianSwap => asianSwap.Notional,
            AsianSwapStrip asianSwapStrip => asianSwapStrip.Swaplets.Sum(x => x.Notional),
            AsianBasisSwap asianBasisSwap => asianBasisSwap.PaySwaplets.Sum(x => x.Notional),
            FxForward fxForward => fxForward.DomesticQuantity,
            EuropeanOption europeanOption => europeanOption.Notional,
            Forward forward => forward.Notional,
            FuturesOption option => option.ContractQuantity * option.LotSize,
            Future future => future.ContractQuantity * future.LotSize,
            CashBalance cash => cash.Notional,
            FixedRateLoanDeposit loanDeposit => loanDeposit.Notional,
            FloatingRateLoanDepo loanDepositFl => loanDepositFl.Notional,
            CashWrapper wrapper => wrapper.UnderlyingInstrument.Notional(),
            ETC etc => etc.Notional,
            CashAsset ca => ca.Notional,
            _ => 0.0,
        };

        private static double SupFacByHedgeSet(string hedgeSet) => hedgeSet == "Electricity" ? 0.4 : 0.18;

        public static Portfolio UnWrapWrappers(this Portfolio portfolio) => new()
        {
            Instruments = portfolio.Instruments.Select(x => x is CashWrapper cw ? cw.UnderlyingInstrument : x).ToList(),
            PortfolioName = portfolio.PortfolioName,
        };

        public static Portfolio UnStripStrips(this Portfolio portfolio) => new()
        {
            Instruments = portfolio.Instruments.SelectMany(x => x is AsianSwapStrip sw ? sw.Swaplets : new[] { x }).ToList(),
            PortfolioName = portfolio.PortfolioName,
        };

        public static double SaCcrAddon(this Portfolio portfolio, IAssetFxModel model, Currency reportingCcy, Dictionary<string, string> AssetIdToHedgingSetMap)
        {
            var pf = portfolio.UnWrapWrappers();

            if (!pf.Instruments.All(x => x is ISaccrEnabled))
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
                foreach (ISaccrEnabled ins in insGroup)
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
            foreach (var hs in activeHedgeSets)
            {
                addOnByHedgeSet[hs.Key] = 0.0;
                var term1 = Pow(correl * hs.Value.Sum(x => x.Value), 2.0);
                var term2 = (1.0 - correl * correl) * hs.Value.Sum(x => x.Value * x.Value);
                addOnByHedgeSet[hs.Key] = Sqrt(term1 + term2);
            }

            var addOn = addOnByHedgeSet.Sum(x => x.Value);

            return addOn;
        }

        public static double SaCcrEAD_old(this Portfolio pf, double EPE, IPvModel model, Currency reportingCcy, Dictionary<string, string> AssetIdToHedgingSetMap)
        {
            var pfe = SaCcrAddon(pf, model.VanillaModel, reportingCcy, AssetIdToHedgingSetMap);
            var rc = EPE;
            var alpha = 1.4;

            return alpha * (rc + pfe);
        }

        public static Portfolio RollWithLifecycle(this Portfolio pf, DateTime rollfromDate, DateTime rollToDate, bool cashOnDayAlreadyPaid = false)
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
                    case FxForward fxf:
                        if (fxf.DeliveryDate < rollToDate && fxf.DeliveryDate >= rollfromDate)
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
                                }
                                break;
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
                            case FloatingRateLoanDepo fl:
                                foreach (var flow in fl.LoanDepoSchedule.Flows.Where(x => x.SettleDate < rollToDate))
                                {
                                    if (((!cashOnDayAlreadyPaid && flow.SettleDate >= rollfromDate) || (cashOnDayAlreadyPaid && flow.SettleDate > rollfromDate)) && flow.SettleDate < rollToDate)
                                        wrapper.CashBalances.Add(new CashBalance(flow.Currency, flow.Fv) { TradeId = i.TradeId + $"{flow.SettleDate}" });
                                }
                                break;
                            case CashAsset ca:
                                if (ca.SettleDate.HasValue && ca.SettleDate < rollToDate && ca.SettleDate >= rollfromDate)
                                {
                                    wrapper.CashBalances.Add(new CashBalance(ca.Currency, -ca.Price.Value * ca.Notional) { TradeId = i.TradeId + "c" });
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
                        o.Instruments.Add(cash.PayDate > rollToDate || cash.Currency.Ccy.StartsWith("X") ?
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

        public static double WeightedMaturity(this Portfolio portfolio, DateTime originDate)
        {
            var d = portfolio.Instruments
                .Select(x => new Tuple<double, double>(originDate.CalculateYearFraction(x.LastSensitivityDate, DayCountBasis.Act365F), Abs(x.Notional())))
                .Where(t => t.Item1 >= 0);
            if (!d.Any())
                return 0.0;
            var totalNotional = d.Sum(x => x.Item2);
            var m = d.Sum(x => x.Item1 * x.Item2) / totalNotional;
            return m;
        }

        public static Portfolio FilterOnSettleDate(this Portfolio pf, DateTime filterOutOnOrBefore)
            => new()
            { Instruments = pf.Instruments.Where(x => x.LastSensitivityDate > filterOutOnOrBefore || (x is CashBalance)).ToList() };

        public static DateTime[] ComputeSimDates(this Portfolio pf, DateTime anchorDate, DatePeriodType frequency = DatePeriodType.Month)
        {
            var removeDateWhenCloserThan = 7;

            var gridDates = new List<DateTime>();
            var finalDate = pf.LastSensitivityDate;

            if (frequency == DatePeriodType.Month)
            {
                var nMonths = (finalDate.Year * 12 + finalDate.Month) - (anchorDate.Year * 12 + anchorDate.Month);
                if (nMonths > 0)
                    gridDates = Enumerable.Range(1, nMonths)
                        .Select(x => anchorDate.LastDayOfMonth().AddMonths(x))
                        .ToList();
            }
            else if (frequency == DatePeriodType.Week)
            {
                var nWeeks = (int)((finalDate - anchorDate).TotalDays / 7) + 1;
                if (nWeeks > 0)
                    gridDates = Enumerable.Range(1, nWeeks)
                        .Select(x => anchorDate.AddDays(x * 7))
                        .ToList();
            }
            else
                throw new Exception("Only support weekly or monthly date generation");

            var payDates = pf.Instruments
                .Select(x => x.LastSensitivityDate.AddDays(-1))
                .Where(x => x != anchorDate);
            gridDates.Add(anchorDate);
            gridDates.Add(finalDate);
            var combinedDates = payDates.Concat(gridDates).Distinct().OrderBy(x => x).ToList();

            var o = new List<DateTime>
            {
                anchorDate
            };
            foreach (var d in combinedDates)
            {
                if (d.Subtract(o.Last()).TotalDays > removeDateWhenCloserThan)
                {
                    o.Add(d);
                }
            }
            return o.ToArray();
        }
    }
}
