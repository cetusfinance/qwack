using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Options.VolSurfaces;

namespace Qwack.Models.Risk.Mutators
{
    public class SurfaceShiftMutator
    {
        public static IAssetFxModel AssetSurfaceShiftRelative(string assetId, double[] shiftSizes, IAssetFxModel model)
        {
            var o = model.Clone();
            var surf = model.GetVolSurface(assetId);
            switch (surf)
            {
                case RiskyFlySurface rf:
                    var bumpedATMs = rf.ATMs.Select((p, ix) => ix < shiftSizes.Length ? p * (1.0 + shiftSizes[ix]) : p * (1.0 + shiftSizes.Last())).ToArray();
                    var rfb = new RiskyFlySurface(rf.OriginDate, bumpedATMs, rf.Expiries, rf.WingDeltas, rf.Riskies, rf.Flies, rf.Forwards, rf.WingQuoteType, rf.AtmVolType, rf.StrikeInterpolatorType, rf.TimeInterpolatorType, rf.PillarLabels)
                    {
                        AssetId = rf.AssetId,
                        Name = rf.Name,
                        Currency = rf.Currency,
                    };
                    o.AddVolSurface(assetId, rfb);
                    break;
                case GridVolSurface gf:
                    var bumpedGrid = gf.Volatilities.Select((r, ix) => r.Select(c => c * (1.0 + shiftSizes[ix])).ToArray()).ToArray();
                    var gfb = new GridVolSurface(gf.OriginDate, gf.Strikes, gf.Expiries, bumpedGrid, gf.StrikeType, gf.StrikeInterpolatorType, gf.TimeInterpolatorType, gf.TimeBasis, gf.PillarLabels)
                    {
                        AssetId = gf.AssetId,
                        Name = gf.Name,
                        Currency = gf.Currency,
                    };
                    o.AddVolSurface(assetId, gfb);
                    break;
                case SparsePointSurface sp:
                    var surfDates = sp.Vols.Keys.Select(x => x.expiry).Distinct().OrderBy(x => x).ToList();
                    var bumpedVols = new Dictionary<(DateTime expiry, double strike), double>();
                    foreach (var kv in sp.Vols)
                    {
                        var expiryIx = surfDates.IndexOf(kv.Key.expiry);
                        var bump = shiftSizes[expiryIx];
                        bumpedVols[kv.Key] = kv.Value * (1.0 + bump);
                    }
                    var spb = new SparsePointSurface(sp.OriginDate, bumpedVols, sp.PillarLabels)
                    {
                        AssetId = sp.AssetId,
                        Name = sp.Name,
                        Currency = sp.Currency,
                    };
                    o.AddVolSurface(assetId, spb);
                    break;
                default:
                    break;
            }
            return o;
        }

        public static IPvModel AssetSurfaceShiftRelative(string assetId, double[] shiftSizes, IPvModel model)
        {
            var newVanillaModel = AssetSurfaceShiftRelative(assetId, shiftSizes, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }

        public static IAssetFxModel AssetSurfaceShiftAbsolute(string assetId, double[] shiftSizes, IAssetFxModel model)
        {
            var o = model.Clone();
            var surf = model.GetVolSurface(assetId);
            switch (surf)
            {
                case RiskyFlySurface rf:
                    var bumpedATMs = rf.ATMs.Select((p, ix) => ix < shiftSizes.Length ? p + shiftSizes[ix] : p + shiftSizes.Last()).ToArray();
                    var rfb = new RiskyFlySurface(rf.OriginDate, bumpedATMs, rf.Expiries, rf.WingDeltas, rf.Riskies, rf.Flies, rf.Forwards, rf.WingQuoteType, rf.AtmVolType, rf.StrikeInterpolatorType, rf.TimeInterpolatorType, rf.PillarLabels)
                    {
                        AssetId = rf.AssetId,
                        Name = rf.Name,
                        Currency = rf.Currency,
                    };
                    o.AddVolSurface(assetId, rfb);
                    break;
                case GridVolSurface gf:
                    var bumpedGrid = gf.Volatilities.Select((r, ix) => r.Select(c => c + shiftSizes[ix]).ToArray()).ToArray();
                    var gfb = new GridVolSurface(gf.OriginDate, gf.Strikes, gf.Expiries, bumpedGrid, gf.StrikeType, gf.StrikeInterpolatorType, gf.TimeInterpolatorType, gf.TimeBasis, gf.PillarLabels)
                    {
                        AssetId = gf.AssetId,
                        Name = gf.Name,
                        Currency = gf.Currency,
                    };
                    o.AddVolSurface(assetId, gfb);
                    break;
                case SparsePointSurface sp:
                    var surfDates = sp.Vols.Keys.Select(x => x.expiry).Distinct().OrderBy(x => x).ToList();
                    var bumpedVols = new Dictionary<(DateTime expiry, double strike), double>();
                    foreach (var kv in sp.Vols)
                    {
                        var expiryIx = surfDates.IndexOf(kv.Key.expiry);
                        var bump = shiftSizes[expiryIx];
                        bumpedVols[kv.Key] = kv.Value + bump;
                    }
                    var spb = new SparsePointSurface(sp.OriginDate, bumpedVols, sp.PillarLabels)
                    {
                        AssetId = sp.AssetId,
                        Name = sp.Name,
                        Currency = sp.Currency,
                    };
                    o.AddVolSurface(assetId, spb);
                    break;
                default:
                    throw new Exception("Unable to mutate curve type");
            }
            return o;
        }

        public static IPvModel AssetSurfaceShiftAbsolute(string assetId, double[] shiftSizes, IPvModel model)
        {
            var newVanillaModel = AssetSurfaceShiftAbsolute(assetId, shiftSizes, model.VanillaModel);
            return model.Rebuild(newVanillaModel, model.Portfolio);
        }
    }
}
