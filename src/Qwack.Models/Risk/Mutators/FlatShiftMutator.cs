using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Core.Models;

namespace Qwack.Models.Risk.Mutators
{
    public class FlatShiftMutator
    {
        public static IAssetFxModel AssetCurveShift(string assetId, double shiftSize, IAssetFxModel model)
        {
            var o = model.Clone();
            var curve = model.GetPriceCurve(assetId);
            switch(curve)
            {
                case PriceCurve pc:
                    var npc = new PriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select(p => p + shiftSize).ToArray(), pc.CurveType, pc.CurrencyProvider, pc.PillarLabels)
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
      
    }
}
