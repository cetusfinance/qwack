using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Models
{
    public class AssetFxModel : IAssetFxModel
    {
        private Dictionary<string, IVolSurface> _assetVols;
        private Dictionary<string, IPriceCurve> _assetCurves;
        private Dictionary<string, IFixingDictionary> _fixings;
        private DateTime _buildDate;
        private IFundingModel _fundingModel;

        public IFundingModel FundingModel => _fundingModel;
        public DateTime BuildDate => _buildDate;

        public ICorrelationMatrix CorrelationMatrix { get; set; }

        public AssetFxModel(DateTime buildDate, IFundingModel fundingModel)
        {
            _assetCurves = new Dictionary<string, IPriceCurve>();
            _assetVols = new Dictionary<string, IVolSurface>();
            _fixings = new Dictionary<string, IFixingDictionary>();
            _buildDate = buildDate;
            _fundingModel = fundingModel;
        }

        public void AddPriceCurve(string name, IPriceCurve curve) => _assetCurves[name] = curve;

        public void AddVolSurface(string name, IVolSurface surface) => _assetVols[name] = surface;

        public IPriceCurve GetPriceCurve(string name)
        {
            if (!_assetCurves.TryGetValue(name, out var curve))
                throw new Exception($"Curve with name {name} not found");
            return curve;
        }

        public IVolSurface GetVolSurface(string name)
        {
            if (!_assetVols.TryGetValue(name, out var surface))
                throw new Exception($"Vol surface with name {name} not found");
            return surface;
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
        public string[] VolSurfaceNames => _assetVols.Keys.Select(x => x).ToArray();
        public string[] FixingDictionaryNames => _fixings.Keys.Select(x => x).ToArray();

        public double GetVolForStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var fwd = _assetCurves[name].GetPriceForDate(expiry); //needs to account for spot/fwd offset
            var vol = _assetVols[name].GetVolForAbsoluteStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var fwd = _assetCurves[name].GetPriceForDate(expiry); //needs to account for spot/fwd offset
            var vol = _assetVols[name].GetVolForDeltaStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetFxVolForStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var pair = FundingModel.FxMatrix.GetFxPair(name);
            var fwd = FundingModel.GetFxRate(expiry.SpotDate(pair.SpotLag, pair.SettlementCalendar, pair.SettlementCalendar), pair.Domestic, pair.Foreign); //needs to account for spot/fwd offset
            var vol = FundingModel.VolSurfaces[name].GetVolForAbsoluteStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetFxVolForDeltaStrikeAndDate(string name, DateTime expiry, double strike)
        {
            var pair = FundingModel.FxMatrix.GetFxPair(name);
            var fwd = FundingModel.GetFxRate(expiry.SpotDate(pair.SpotLag, pair.SettlementCalendar, pair.SettlementCalendar), pair.Domestic, pair.Foreign); //needs to account for spot/fwd offset
            var vol = FundingModel.VolSurfaces[name].GetVolForDeltaStrike(strike, expiry, fwd);
            return vol;
        }

        public double GetAverageVolForStrikeAndDates(string name, DateTime[] expiries, double strike)
        {
            var surface = _assetVols[name];
            var ts = expiries.Select(expiry => _buildDate.CalculateYearFraction(expiry, DayCountBasis.Act365F)).ToArray();
            var fwds = expiries.Select(expiry=>_assetCurves[name].GetPriceForDate(expiry)); //needs to account for spot/fwd offset
            var vols = fwds.Select((fwd,ix)=> surface.GetVolForAbsoluteStrike(strike, expiries[ix], fwd));
            var varianceAvg = vols.Select((v, ix) => v * v * ts[ix]).Sum();
            varianceAvg /= ts.Sum();
            var sigma = System.Math.Sqrt(varianceAvg/ts.Average());
            return sigma;
        }

        public double GetAverageVolForMoneynessAndDates(string name, DateTime[] expiries, double moneyness)
        {
            var surface = _assetVols[name];
            var ts = expiries.Select(expiry => _buildDate.CalculateYearFraction(expiry, DayCountBasis.Act365F)).ToArray();
            var fwds = expiries.Select(expiry => _assetCurves[name].GetPriceForDate(expiry)); //needs to account for spot/fwd offset
            var vols = fwds.Select((fwd, ix) => surface.GetVolForAbsoluteStrike(fwd * moneyness, expiries[ix], fwd));
            var varianceAvg = vols.Select((v, ix) => v * v * ts[ix]).Sum();
            varianceAvg /= ts.Sum();
            var sigma = System.Math.Sqrt(varianceAvg / ts.Average());
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
                _assetVols[kv.Key] = kv.Value;
        }

        public void AddFixingDictionaries(Dictionary<string, IFixingDictionary> fixings)
        {
            foreach (var kv in fixings)
                _fixings[kv.Key] = kv.Value;
        }
    }
}
