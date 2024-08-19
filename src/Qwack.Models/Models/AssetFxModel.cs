using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Correlation;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Options.VolSurfaces;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Models
{
    public class AssetFxModel : IAssetFxModel
    {
        private readonly Dictionary<VolSurfaceKey, IVolSurface> _assetVols;
        private readonly Dictionary<string, IPriceCurve> _assetCurves;
        private readonly Dictionary<string, IFixingDictionary> _fixings;
        private DateTime _buildDate;
        private readonly IFundingModel _fundingModel;

        public IFundingModel FundingModel => _fundingModel;
        public DateTime BuildDate => _buildDate;

        public IPriceCurve[] Curves => _assetCurves.Values.ToArray();

        public ICorrelationMatrix CorrelationMatrix { get; set; }

        public AssetFxModel(DateTime buildDate, IFundingModel fundingModel)
        {
            _assetCurves = new Dictionary<string, IPriceCurve>();
            _assetVols = new Dictionary<VolSurfaceKey, IVolSurface>();
            _fixings = new Dictionary<string, IFixingDictionary>();
            _buildDate = buildDate;
            _fundingModel = fundingModel;
        }

        public AssetFxModel(TO_AssetFxModel transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
            : this(transportObject.BuildDate, new FundingModel(transportObject.FundingModel, currencyProvider, calendarProvider))
        {
            _assetCurves = transportObject.AssetCurves?.ToDictionary(x => x.Key, x => x.Value.GetPriceCurve(currencyProvider, calendarProvider)) ?? new Dictionary<string, IPriceCurve>();
            _assetVols = transportObject.AssetVols?.ToDictionary(x => new VolSurfaceKey(x.Key, currencyProvider), y => VolSurfaceFactory.GetVolSurface(y.Value, currencyProvider)) ?? new Dictionary<VolSurfaceKey, IVolSurface>();
            _fixings = transportObject.Fixings?.ToDictionary(x => x.Key, x => (IFixingDictionary)new FixingDictionary(x.Value)) ?? new Dictionary<string, IFixingDictionary>();
            CorrelationMatrix = transportObject.CorrelationMatrix == null ? null : CorrelationMatrixFactory.GetCorrelationMatrix(transportObject.CorrelationMatrix);
        }

        public void AddPriceCurve(string name, IPriceCurve curve) => _assetCurves[name ?? curve.AssetId] = curve;

        public void AddVolSurface(string name, IVolSurface surface)
        {
            if (IsFx(name))
                FundingModel.VolSurfaces[name] = surface;
            else
                _assetVols[new VolSurfaceKey(surface.AssetId, surface.Currency)] = surface;

        }
        public void AddVolSurface(VolSurfaceKey key, IVolSurface surface) => _assetVols[key] = surface;

        public static bool IsFx(string name) => (name.Length == 7 && name[3] == '/') || (name.Length == 8 && name[4] == '/');

        public IPriceCurve GetPriceCurve(string name, Currency currency = null)
        {
            if (IsFx(name))
            {
                var ccys = name.Split('/');
                return new FxForwardCurve(BuildDate, FundingModel, FundingModel.GetCurrency(ccys[0]), FundingModel.GetCurrency(ccys[1]));
            }
            if (!_assetCurves.TryGetValue(name, out var curve))
                throw new Exception($"Curve with name {name} not found");

            if (currency != null && curve.Currency != currency)
                return new CompositePriceCurve(curve.BuildDate, curve, FundingModel, currency);

            return curve;
        }

        public IVolSurface GetVolSurface(string name, Currency currency = null)
        {
            if (IsFx(name))
                return FundingModel.GetVolSurface(name);

            if (currency != null)
            {
                if (!_assetVols.TryGetValue(new VolSurfaceKey(name, currency), out var surface))
                    throw new Exception($"Vol surface {name}/{currency} not found");

                return surface;
            }

            var surfaces = _assetVols.Where(x => name.Contains("~") ? x.Key.ToString() == name : x.Key.AssetId == name);
            if (!surfaces.Any())
                throw new Exception($"Vol surface {name} not found");

            return surfaces.First().Value;
        }

        public bool TryGetVolSurface(string name, out IVolSurface surface, Currency currency = null)
        {
            surface = null;
            if (IsFx(name))
                return FundingModel.TryGetVolSurface(name, out surface);

            if (currency != null)
                return _assetVols.TryGetValue(new VolSurfaceKey(name, currency), out surface);

            surface = _assetVols.Where(x => name.Contains("~") ? x.Key.ToString() == name : x.Key.AssetId == name).FirstOrDefault().Value;
            return surface != default(IVolSurface);
        }

        public void AddFixingDictionary(string name, IFixingDictionary fixings) => _fixings[name] = fixings;

        public IFixingDictionary GetFixingDictionary(string name)
        {
            if (!_fixings.TryGetValue(name, out var dict))
                throw new Exception($"Fixing dictionary with name {name} not found");
            return dict;
        }

        public bool TryGetFixingDictionary(string name, out IFixingDictionary fixings) => _fixings.TryGetValue(name, out fixings);

        public string[] CurveNames => _assetCurves.Keys.Select(x => x).ToArray();
        public string[] VolSurfaceNames => _assetVols.Keys.Select(x => x.AssetId).ToArray();
        public string[] FixingDictionaryNames => _fixings.Keys.Select(x => x).ToArray();

        public IAssetFxModel VanillaModel => this;

        public double GetVolForStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var surface = GetVolSurface(name);
            var curve = GetPriceCurve(name);
            var fwd = surface.OverrideSpotLag == null ?
                curve.GetPriceForFixingDate(expiry) :
                curve.GetPriceForDate(expiry.AddPeriod(RollType.F, curve.SpotCalendar, surface.OverrideSpotLag));
            var vol = surface.GetVolForAbsoluteStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetVolForStrikeAndDate(string name, DateTime expiry, double strike, double fwd)
        {
            var surface = GetVolSurface(name);
            var vol = surface.GetVolForAbsoluteStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var fwd = GetPriceCurve(name).GetPriceForFixingDate(expiry); //needs to account for spot/fwd offset
            var vol = GetVolSurface(name).GetVolForDeltaStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetFxVolForStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var pair = FundingModel.FxMatrix.GetFxPair(name);
            var fwd = FundingModel.GetFxRate(expiry.SpotDate(pair.SpotLag, pair.PrimaryCalendar, pair.PrimaryCalendar), pair.Domestic, pair.Foreign); //needs to account for spot/fwd offset
            var vol = FundingModel.GetVolSurface(name).GetVolForAbsoluteStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetFxVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var pair = FundingModel.FxMatrix.GetFxPair(name);
            var fwd = FundingModel.GetFxRate(expiry.SpotDate(pair.SpotLag, pair.PrimaryCalendar, pair.PrimaryCalendar), pair.Domestic, pair.Foreign); //needs to account for spot/fwd offset
            var vol = FundingModel.GetVolSurface(name).GetVolForDeltaStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetAverageVolForStrikeAndDates(string name, DateTime[] expiries, double strike)
        {
            var surface = GetVolSurface(name);
            var ts = expiries.Select(expiry => _buildDate.CalculateYearFraction(expiry, DayCountBasis.Act365F)).ToArray();
            var fwds = expiries.Select(expiry => _assetCurves[name].GetPriceForDate(expiry)); //needs to account for spot/fwd offset
            var vols = fwds.Select((fwd, ix) => surface.GetVolForAbsoluteStrike(strike, expiries[ix], fwd));
            var varianceAvg = vols.Select((v, ix) => v * v * ts[ix]).Sum();
            varianceAvg /= ts.Sum();
            var sigma = System.Math.Sqrt(varianceAvg / ts.Average());
            return sigma;
        }

        public double GetAverageVolForMoneynessAndDates(string name, DateTime[] expiries, double moneyness)
        {
            var surface = GetVolSurface(name);
            var ts = expiries.Select(expiry => _buildDate.CalculateYearFraction(expiry, DayCountBasis.Act365F)).ToArray();
            var fwds = expiries.Select(expiry => _assetCurves[name].GetPriceForDate(expiry)); //needs to account for spot/fwd offset
            var vols = fwds.Select((fwd, ix) => surface.GetVolForAbsoluteStrike(fwd * moneyness, expiries[ix], fwd));
            var variances = vols.Select((v, ix) => v * v * ts[ix]).ToArray();
            var varianceWeightedVol = vols.Select((v, ix) => v * variances[ix]).Sum() / variances.Sum();

            return varianceWeightedVol;
        }

        public double GetCompositeVolForStrikeAndDate(string assetId, DateTime expiry, double strike, Currency ccy)
        {
            var curve = GetPriceCurve(assetId);

            if (curve.Currency == ccy)
                return GetVolForStrikeAndDate(assetId, expiry, strike);

            var fxId = $"{curve.Currency.Ccy}/{ccy.Ccy}";
            var fxPair = FundingModel.FxMatrix.GetFxPair(fxId);

            var fxSpotDate = fxPair.SpotDate(expiry);
            var fxFwd = FundingModel.GetFxRate(fxSpotDate, fxId);
            var fxVol = FundingModel.GetVolSurface(fxId).GetVolForDeltaStrike(0.5, expiry, fxFwd);
            var tExpC = BuildDate.CalculateYearFraction(expiry, DayCountBasis.Act365F);
            var correl = CorrelationMatrix?.GetCorrelation(fxId, assetId, tExpC) ?? 0.0;
            var sigma = GetVolForStrikeAndDate(assetId, expiry, strike / fxFwd);
            sigma = System.Math.Sqrt(sigma * sigma + fxVol * fxVol + 2 * correl * fxVol * sigma);
            return sigma;
        }

        public IAssetFxModel Clone()
        {
            var c = new AssetFxModel(BuildDate, FundingModel.DeepClone(null));

            foreach (var kv in _assetCurves)
                c.AddPriceCurve(kv.Key, kv.Value);

            foreach (var kv in _assetVols)
                c.AddVolSurface(kv.Key, kv.Value);

            foreach (var kv in _fixings)
                c.AddFixingDictionary(kv.Key, kv.Value);

            c.CorrelationMatrix = CorrelationMatrix;
            c.AttachPortfolio(_portfolio);
            return c;
        }

        public IAssetFxModel Clone(IFundingModel fundingModel)
        {
            var c = new AssetFxModel(BuildDate, fundingModel);

            foreach (var kv in _assetCurves)
                c.AddPriceCurve(kv.Key, kv.Value);

            foreach (var kv in _assetVols)
                c.AddVolSurface(kv.Key, kv.Value);

            foreach (var kv in _fixings)
                c.AddFixingDictionary(kv.Key, kv.Value);

            c.CorrelationMatrix = CorrelationMatrix;
            c.AttachPortfolio(_portfolio);

            return c;
        }

        public void AddPriceCurves(Dictionary<string, IPriceCurve> curves)
        {
            foreach (var kv in curves)
                _assetCurves[kv.Key] = kv.Value;
        }

        public void AddVolSurfaces(Dictionary<string, IVolSurface> surfaces)
        {
            foreach (var kv in surfaces)
                AddVolSurface(new VolSurfaceKey(kv.Value.AssetId, kv.Value.Currency), kv.Value);
        }

        public void AddFixingDictionaries(Dictionary<string, IFixingDictionary> fixings)
        {
            foreach (var kv in fixings)
                _fixings[kv.Key] = kv.Value;
        }

        private Portfolio _portfolio;
        public Portfolio Portfolio => _portfolio;
        public void AttachPortfolio(Portfolio portfolio) => _portfolio = portfolio;
        public ICube PV(Currency reportingCurrency) => _portfolio.PV(this, reportingCurrency);
        public ICube ParRate(Currency reportingCurrency) => _portfolio.ParRate(this, reportingCurrency);

        public IPvModel Rebuild(IAssetFxModel newVanillaModel, Portfolio portfolio)
        {
            var m = newVanillaModel.Clone();
            m.AttachPortfolio(portfolio);
            return m;
        }

        private Dictionary<string, string[]> _dependencyTree;
        private Dictionary<string, string[]> _dependencyTreeFull;

        public void BuildDependencyTree()
        {
            if (_dependencyTree != null)
                return;

            _dependencyTree = new Dictionary<string, string[]>();
            _dependencyTreeFull = new Dictionary<string, string[]>();
            foreach (var curveName in CurveNames)
            {
                var curveObj = GetPriceCurve(curveName);
                var linkedCurves = Curves
                    .Where(x => x is BasisPriceCurve bp && bp.BaseCurve.Name == curveName)
                    .Select(x => x.Name)
                    .ToArray();

                _dependencyTree.Add(curveName, linkedCurves);
            }

            foreach (var kv in _dependencyTree)
            {
                var fullDeps = kv.Value;
                var newDeps = new List<string>();
                foreach (var dep in kv.Value)
                {
                    if (_dependencyTree.TryGetValue(dep, out var deps))
                    {
                        newDeps.AddRange(deps);
                    }
                }

                while (newDeps.Any())
                {
                    var actualNewDeps = newDeps.Where(x => !fullDeps.Contains(x));
                    fullDeps = fullDeps.Concat(actualNewDeps).Distinct().ToArray();

                    newDeps = new List<string>();
                    foreach (var dep in actualNewDeps)
                    {
                        if (_dependencyTree.TryGetValue(dep, out var deps))
                        {
                            newDeps.AddRange(deps);
                        }
                    }
                }

                _dependencyTreeFull.Add(kv.Key, fullDeps);
            }
        }

        public string[] GetDependentCurves(string curve) => _dependencyTree.TryGetValue(curve, out var values) ? values : throw new Exception($"Curve {curve} not found");
        public string[] GetAllDependentCurves(string curve) => _dependencyTreeFull.TryGetValue(curve, out var values) ? values : throw new Exception($"Curve {curve} not found");

        public void OverrideBuildDate(DateTime buildDate) => _buildDate = buildDate;

        public TO_AssetFxModel ToTransportObject()
        {
            var returnObject = new TO_AssetFxModel();
            returnObject.AssetCurves = _assetCurves?.ToDictionary(x => x.Key, x => x.Value.GetTransportObject());
            returnObject.AssetVols = _assetVols?.ToDictionary(x => x.Key.GetTransportObject(), x => x.Value.GetTransportObject());
            returnObject.BuildDate = BuildDate;
            returnObject.CorrelationMatrix = CorrelationMatrix?.GetTransportObject();
            returnObject.Fixings = _fixings?.ToDictionary(x => x.Key, x => x.Value.GetTransportObject());
            returnObject.FundingModel = _fundingModel.GetTransportObject();
            returnObject.Portfolio = _portfolio?.ToTransportObject();
            return returnObject;
        }



        public void RemovePriceCurve(IPriceCurve curve) => _assetCurves.Remove(curve.Name);

        public void RemoveVolSurface(IVolSurface surface)
        {
            var key = new VolSurfaceKey(surface.AssetId, surface.Currency);
            _assetVols.Remove(key);
        }

        public void RemoveFixingDictionary(string name) => _fixings.Remove(name);

        public IAssetFxModel TrimModel(Portfolio portfolio, string[] additionalIrCurves = null, string[] additionalCcys = null)
        {
            var o = Clone();
            var assetIds = portfolio.AssetIds();
            var pairs = portfolio.FxPairs(o);
            var ccys = pairs.SelectMany(x => x.Split('/')).Distinct().ToArray();
            if (additionalCcys != null)
            {
                ccys = ccys.Concat(additionalCcys).Distinct().ToArray();
            }
            var irCurves = portfolio.Instruments
                .Where(x => x is IAssetInstrument).SelectMany(x => (x as IAssetInstrument).IrCurves(o))
                .Concat(ccys.Select(c => o.FundingModel.FxMatrix.DiscountCurveMap.Single(dc => dc.Key.Ccy == c).Value))
                .Distinct()
                .ToArray();

            var surplusCurves = o.CurveNames.Where(x => !assetIds.Contains(x)).ToArray();
            var surplusVols = o.VolSurfaceNames.Where(x => !assetIds.Contains(x)).ToArray();
            var surplusFixings = o.FixingDictionaryNames.Where(x => !assetIds.Contains(x) && !pairs.Contains(x)).ToArray();
            var surplusIrCurves = o.FundingModel.Curves.Keys.Where(x => !irCurves.Contains(x)).ToArray();
            if (additionalIrCurves != null)
            {
                surplusIrCurves = surplusIrCurves.Where(ir => !additionalIrCurves.Contains(ir)).ToArray();
            }
            var surplusFxRates = o.FundingModel.FxMatrix.SpotRates.Keys.Where(x => !ccys.Contains(x.Ccy)).ToArray();
            foreach (var s in surplusCurves)
            {
                o.RemovePriceCurve(o.GetPriceCurve(s));
            }
            foreach (var s in surplusVols)
            {
                o.RemoveVolSurface(o.GetVolSurface(s));
            }
            foreach (var s in surplusFixings)
            {
                o.RemoveFixingDictionary(s);
            }
            foreach (var s in surplusIrCurves)
            {
                o.FundingModel.Curves.Remove(s);
            }
            foreach (var s in surplusFxRates)
            {
                o.FundingModel.FxMatrix.SpotRates.Remove(s);
            }
            return o;
        }

        public double GetCorrelation(string label1, string label2, double t = 0)
        {
            if (CorrelationMatrix == null)
                throw new Exception("No correlation matrix attached to model");

            return CorrelationMatrix.GetCorrelation(label1, label2, t);
        }

        public void Dispose() 
        {
        } 
    }
}
