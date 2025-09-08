using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;

namespace Qwack.Models.Risk.Mutators
{
    public class RelativeShiftMutator
    {
        public static IAssetFxModel AssetCurveShift(string assetId, double shiftSize, IAssetFxModel model)
        {
            var o = model.Clone();
            var curve = model.GetPriceCurve(assetId);
            switch (curve)
            {
                case BasicPriceCurve pc:
                    var npc = new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select(p => p * (1.0 + shiftSize)).ToArray(), pc.CurveType, pc.CurrencyProvider, pc.PillarLabels)
                    {
                        AssetId = pc.AssetId,
                        Name = pc.Name,
                        CollateralSpec = pc.CollateralSpec,
                        Currency = pc.Currency,
                        Units = pc.Units,
                        RefDate = pc.RefDate,
                    };
                    o.AddPriceCurve(assetId, npc);
                    break;
                case EquityPriceCurve eq:
                    var neq = new EquityPriceCurve(eq.BuildDate, eq.Spot * (1.0 + shiftSize), eq.Currency, eq.IrCurve, eq.SpotDate, eq.PillarDates, eq.DivYields, eq.DiscreteDivDates, eq.DiscreteDivs, eq.CurrencyProvider, eq.Basis, eq.PillarLabels)
                    {
                        AssetId = eq.AssetId,
                        Name = eq.Name,
                        Currency = eq.Currency,
                        Units = eq.Units,
                    };
                    o.AddPriceCurve(assetId, neq);
                    break;
                case ContangoPriceCurve cp:
                    var ncp = new ContangoPriceCurve(cp.BuildDate, cp.Spot * (1.0 + shiftSize), cp.SpotDate, cp.PillarDates, cp.Contangos, cp.CurrencyProvider, cp.Basis, cp.PillarLabels)
                    {
                        AssetId = cp.AssetId,
                        Name = cp.Name,
                        Currency = cp.Currency,
                        Units = cp.Units,
                    };
                    o.AddPriceCurve(assetId, ncp);
                    break;
                default:
                    throw new Exception("Unable to mutate curve type");
            }
            return o;
        }
        public static IPvModel AssetCurveShift(string assetId, double shiftSize, IPvModel model)
        {
            var newVanillaModel = AssetCurveShift(assetId, shiftSize, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }

        public static IAssetFxModel SpotFxShift(string ccy, double shiftSize, IAssetFxModel model)
        {
            var o = model.Clone();
            var spotRates = new Dictionary<Currency, double>(o.FundingModel.FxMatrix.SpotRates);
            var currency = spotRates.Keys.FirstOrDefault(c => c.Ccy == ccy);
            if (currency != null)
            {
                spotRates[currency] *= (1.0 + shiftSize);
                o.FundingModel.FxMatrix.UpdateSpotRates(spotRates);
            }

            return o;
        }

        public static IPvModel SpotFxShift(string ccy, double shiftSize, IPvModel model)
        {
            var newVanillaModel = SpotFxShift(ccy, shiftSize, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }

        public static IAssetFxModel FxSpotShift(Currency ccy, double shiftSize, IAssetFxModel model)
        {
            var o = model.Clone();
            var spot = model.FundingModel.FxMatrix.GetSpotRate(ccy);
            o.FundingModel.FxMatrix.SpotRates[ccy] = spot * (1.0 + shiftSize);
            return o;
        }

        public static IPvModel FxSpotShift(Currency ccy, double shiftSize, IPvModel model)
        {
            var newVanillaModel = FxSpotShift(ccy, shiftSize, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }

        public static IAssetFxModel FxSpotShift(FxPair pair, double shiftSize, IAssetFxModel model)
        {
            var o = model.Clone();
            if (pair.Domestic == model.FundingModel.FxMatrix.BaseCurrency)
            {
                var spot = model.FundingModel.FxMatrix.GetSpotRate(pair.Foreign);
                o.FundingModel.FxMatrix.SpotRates[pair.Foreign] = spot * (1.0 + shiftSize);
            }
            else if (pair.Foreign == model.FundingModel.FxMatrix.BaseCurrency)
            {
                var spot = model.FundingModel.FxMatrix.GetSpotRate(pair.Domestic);
                o.FundingModel.FxMatrix.SpotRates[pair.Domestic] = 1 / (1 / (spot * (1.0 + shiftSize)));
            }
            else
            {
                throw new Exception("Shifted FX pair must contain base currency of model");
            }

            return o;
        }

        public static IPvModel FxSpotShift(FxPair pair, double shiftSize, IPvModel model)
        {
            var newVanillaModel = FxSpotShift(pair, shiftSize, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }



    }
}
