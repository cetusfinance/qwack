using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;

namespace Qwack.Models.Risk.Mutators
{
    public class CurveShiftMutator
    {
        public static IAssetFxModel AssetCurveShiftRelative(string assetId, double[] shiftSizes, IAssetFxModel model)
        {
            var o = model.Clone();
            var curve = model.GetPriceCurve(assetId);
            switch(curve)
            {
                case BasicPriceCurve pc:
                    var npc = new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p,ix) => ix<shiftSizes.Length ? p * (1.0 + shiftSizes[ix]): p * (1.0+ shiftSizes.Last())).ToArray(), pc.CurveType, pc.CurrencyProvider, pc.PillarLabels)
                    {
                        AssetId = pc.AssetId,
                        Name = pc.Name,
                        CollateralSpec = pc.CollateralSpec,
                        Currency = pc.Currency,
                    };
                    o.AddPriceCurve(assetId, npc);
                    break;
                default:
                    throw new Exception("Unable to mutate curve type");
            }
            return o;
        }

        public static IPvModel AssetCurveShiftRelative(string assetId, double[] shiftSizes, IPvModel model)
        {
            var newVanillaModel = AssetCurveShiftRelative(assetId, shiftSizes, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }

        public static IAssetFxModel AssetCurveShiftAbsolute(string assetId, double[] shiftSizes, IAssetFxModel model)
        {
            var o = model.Clone();
            var curve = model.GetPriceCurve(assetId);
            switch (curve)
            {
                case BasicPriceCurve pc:
                    var npc = new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p, ix) => ix < shiftSizes.Length ? p + shiftSizes[ix] : p + shiftSizes.Last()).ToArray(), pc.CurveType, pc.CurrencyProvider, pc.PillarLabels)
                    {
                        AssetId = pc.AssetId,
                        Name = pc.Name,
                        CollateralSpec = pc.CollateralSpec,
                        Currency = pc.Currency,
                    };
                    o.AddPriceCurve(assetId, npc);
                    break;
                default:
                    throw new Exception("Unable to mutate curve type");
            }
            return o;
        }

        public static IPvModel AssetCurveShiftAbsolute(string assetId, double[] shiftSizes, IPvModel model)
        {
            var newVanillaModel = AssetCurveShiftAbsolute(assetId, shiftSizes, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }
    }
}
