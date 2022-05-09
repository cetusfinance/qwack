using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options;
using Qwack.Options.Asians;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Risk
{
    public static class Basel2Risk
    {
        public static Dictionary<string, double> Delta(IInstrument ins, IAssetFxModel model, Currency repCcy)
        {
            var o = new Dictionary<string, double>();
            switch (ins)
            {
                case EuropeanOption eu:
                    if (eu.ExpiryDate < model.BuildDate)
                        return o;
                    var c0 = model.GetPriceCurve(eu.AssetId);
                    var comFwd0 = c0.GetPriceForFixingDate(eu.ExpiryDate) * model.FundingModel.GetFxRate(eu.ExpiryDate, c0.Currency, eu.Currency);
                    var fxToRep0 = model.FundingModel.GetFxRate(model.BuildDate, eu.Currency, repCcy);
                    var vol = model.GetCompositeVolForStrikeAndDate(eu.AssetId, eu.ExpiryDate, eu.Strike, eu.Currency);
                    var t = model.BuildDate.CalculateYearFraction(eu.ExpiryDate, DayCountBasis.ACT365F);
                    var commoDelta0 = BlackFunctions.BlackDelta(comFwd0, eu.Strike, 0.0, t, vol, eu.CallPut) * fxToRep0;
                    o.Add(eu.AssetId, commoDelta0);
                    if (fxToRep0 != 1)
                    {
                        var pair = $"{eu.Currency}/{repCcy}";
                        o.Add(pair, commoDelta0);
                    }
                    return o;
                case Forward f:
                    if (f.ExpiryDate < model.BuildDate)
                        return o;
                    var c = model.GetPriceCurve(f.AssetId);
                    var comFwd = c.GetPriceForFixingDate(f.ExpiryDate) * model.FundingModel.GetFxRate(f.ExpiryDate, c.Currency, f.Currency);
                    var fxToRep = model.FundingModel.GetFxRate(model.BuildDate, f.Currency, repCcy);
                    var commoDelta = f.Notional * comFwd * fxToRep;
                    o.Add(f.AssetId, commoDelta);
                    if (fxToRep != 1)
                    {
                        var pair = $"{f.Currency}/{repCcy}";
                        o.Add(pair, commoDelta);
                    }
                    return o;
                case AsianOption aso:
                    var c1 = model.GetPriceCurve(aso.AssetId);
                    var comFwd1 = c1.GetAveragePriceForDates(aso.FixingDates) * model.FundingModel.GetFxAverage(aso.FxFixingDates ?? aso.FixingDates, c1.Currency, aso.Currency);
                    var fxToRep1 = model.FundingModel.GetFxRate(model.BuildDate, aso.Currency, repCcy);
                    var volDate = DateTime.FromOADate(0.5 * (aso.AverageStartDate.ToOADate() + aso.AverageEndDate.ToOADate()));
                    var vol1 = model.GetCompositeVolForStrikeAndDate(aso.AssetId, volDate, aso.Strike, aso.Currency);
                    var tS = model.BuildDate.CalculateYearFraction(aso.AverageStartDate, DayCountBasis.ACT365F);
                    var tE = model.BuildDate.CalculateYearFraction(aso.AverageEndDate, DayCountBasis.ACT365F);
                    var commoDelta1 = TurnbullWakeman.Delta(comFwd1, comFwd1, vol1, aso.Strike, tS, tE, 0.0, aso.CallPut) * fxToRep1;
                    o.Add(aso.AssetId, commoDelta1);
                    if (fxToRep1 != 1)
                    {
                        var pair = $"{aso.Currency}/{repCcy}";
                        o.Add(pair, commoDelta1);
                    }
                    return o;
                case AsianSwap ass:
                    if (ass.AverageEndDate < model.BuildDate)
                        return o;

                    var c2 = model.GetPriceCurve(ass.AssetId);
                    var comFwd2 = c2.GetAveragePriceForDates(ass.FixingDates) * model.FundingModel.GetFxAverage(ass.FxFixingDates ?? ass.FixingDates, c2.Currency, ass.Currency);
                    var fxToRep2 = model.FundingModel.GetFxRate(model.BuildDate, ass.Currency, repCcy);
                    var commoDelta2 = ass.Notional * comFwd2 * fxToRep2;

                    if (ass.AverageStartDate > model.BuildDate)
                    {
                        var factor = ass.FixingDates.Count(x => x > model.BuildDate) / ass.FixingDates.Count();
                        commoDelta2 *= factor;
                    }

                    o.Add(ass.AssetId, commoDelta2);
                    if (fxToRep2 != 1)
                    {
                        var pair = $"{ass.Currency}/{repCcy}";
                        o.Add(pair, commoDelta2);
                    }
                    return o;
                default:
                    throw new Exception("Basel2 delta does not support instrument type");
            }
        }
    }
}
