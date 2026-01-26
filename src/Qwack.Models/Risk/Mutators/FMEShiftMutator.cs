using System;
using System.Linq;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Risk.Mutators
{
    public class FMEShiftMutator
    {
        public static IAssetFxModel AssetCurveShift(string assetId, double refShiftSize, double lambda, bool useImpliedVol, IAssetFxModel model)
        {
            var o = model.Clone();
            var curve = model.GetPriceCurve(assetId);
            switch (curve)
            {
                case BasicPriceCurve pc:
                    var shiftFactors = GenerateShiftFactors(pc.PillarDates, assetId, lambda, useImpliedVol, model);
                    var npc = new BasicPriceCurve(pc.BuildDate, pc.PillarDates, pc.Prices.Select((p, ix) => p + shiftFactors[ix] * refShiftSize).ToArray(), pc.CurveType, pc.CurrencyProvider, pc.PillarLabels)
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

        public static double[] GenerateShiftFactors(DateTime[] dates, string assetId, double lambda, bool useImpliedVol, IAssetFxModel model)
        {
            var o = new double[dates.Length];
            var curve = model.GetPriceCurve(assetId);
            var refDate = curve.RefDate;
            var refVol = model.GetVolForDeltaStrikeAndDate(assetId, refDate, 0.5);

            for (var i = 0; i < dates.Length; i++)
            {
                var t = System.Math.Abs(refDate.CalculateYearFraction(dates[i], DayCountBasis.Act365F));
                var correlation  = System.Math.Exp(-lambda * t * t);
                if (useImpliedVol)
                {
                    var vol = model.GetVolForDeltaStrikeAndDate(assetId, dates[i], 0.5);
                    o[i] = vol / refVol * correlation;
                }
                else
                {
                    o[i] = correlation;
                }
            }

            return o;
        }

        public static IPvModel AssetCurveShift(string assetId, double refShiftSize, double lambda, bool useImpliedVol, IPvModel model)
        {
            var newVanillaModel = AssetCurveShift(assetId, refShiftSize, lambda, useImpliedVol, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }
    }
}
