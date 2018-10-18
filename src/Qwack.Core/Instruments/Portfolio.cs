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

        public string TradeId => throw new NotImplementedException();

        public string Counterparty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public static class PortfolioEx
    {
        public static DateTime LastSensitivityDate(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return DateTime.MinValue;

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward);

            if (!fxTrades.Any() && !assetTrades.Any())
                return DateTime.MinValue;
            else if (!fxTrades.Any())
                return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate); 
            else if (!assetTrades.Any())
                return fxTrades.Max(x => ((FxForward)x).DeliveryDate);
            return assetTrades.Max(x => ((IAssetInstrument)x).LastSensitivityDate).Max(fxTrades.Max(x => ((FxForward)x).DeliveryDate));
        }

        public static string[] AssetIds(this Portfolio portfolio)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);

            if(!assetTrades.Any())
                return new string[0];

            return assetTrades.SelectMany(x => ((IAssetInstrument)x).AssetIds).Distinct().ToArray();
        }

        public static string[] FxPairs(this Portfolio portfolio, IAssetFxModel model)
        {
            if (portfolio.Instruments.Count == 0)
                return new string[0];

            var assetTrades = portfolio.Instruments
                .Where(x => x is IAssetInstrument);
            var fxTrades = portfolio.Instruments
                .Where(x => x is FxForward);

            if (!fxTrades.Any() && !assetTrades.Any())
                return new string[0];

            var o = new List<string>();

            if(fxTrades.Any())
            {
                o.AddRange(fxTrades.Select(x => ((FxForward)x).Pair));
            }
            if(assetTrades.Any())
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
                        o[i + 1, 1] = "ListedOption";
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
                        o[i + 1, 2] = l.Ccy.Ccy;
                        o[i + 1, 3] = l.Ccy.Ccy;
                        o[i + 1, 4] = l.StartDate;
                        o[i + 1, 5] = l.EndDate;
                        o[i + 1, 6] = l.EndDate;
                        o[i + 1, 7] = l.InterestRate;
                        o[i + 1, 8] = l.Notional;
                        o[i + 1, 9] = string.Empty;
                        o[i + 1, 10] = l.Counterparty ?? string.Empty;
                        break;
                }
            }

            return o;
        }
    }
}
