using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Options.VolSurfaces
{
    public class SparsePointSurface : IVolSurface, IATMVolSurface
    {
        private readonly Dictionary<(DateTime expiry, double strike), double> _vols = new();
        private readonly Dictionary<DateTime, string> _pillarLabels = new();

        public SparsePointSurface(DateTime origin) => OriginDate = origin;

        public SparsePointSurface(DateTime origin, Dictionary<(DateTime expiry, double strike), double> vols, Dictionary<DateTime, string> labels)
        {
            OriginDate = origin;
            _vols = vols;
            _pillarLabels = labels;
        }

        public SparsePointSurface(TO_SparsePointSurface to, ICurrencyProvider currencyProvider)
        {
            OriginDate = to.OriginDate;
            Name = to.Name;
            AssetId = to.AssetId;
            Currency = currencyProvider.GetCurrencySafe(to.Currency);

            _vols = to.Vols.ToDictionary(kv => (DateTime.Parse(kv.Key.Split('~')[0]), double.Parse(kv.Key.Split('~')[1])), kv => kv.Value);
            _pillarLabels = to.PointLabels.ToDictionary(kv => DateTime.Parse(kv.Key), kv => kv.Value);
        }

        public DateTime OriginDate { get; private set; }
        public DateTime[] Expiries => _vols.Keys.Select(k => k.expiry).Distinct().OrderBy(d => d).ToArray();
        public string Name { get; set; }

        public Currency Currency { get; set; }
        public string AssetId { get; set; }
        public IInterpolator2D LocalVolGrid { get; set; }
        public Frequency OverrideSpotLag { get; set; }

        public Dictionary<(DateTime expiry, double strike), double> Vols => _vols;
        public Dictionary<DateTime, string> PillarLabels => _pillarLabels;

        public double[] GetStrikesForExpiry(DateTime expiry)
        {
            var kvs = _vols.Where(kv => kv.Key.expiry == expiry).ToArray();
            return kvs.Select(kv => kv.Key.strike).ToArray();
        }

        public DateTime PillarDatesForLabel(string label) => _pillarLabels.Where(x => x.Value == label).FirstOrDefault().Key;
        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => _vols[(expiry, strike)];

        public void AddPoint(DateTime expiry, double strike, double vol, string label)
        {
            _vols[(expiry, strike)] = vol;
            _pillarLabels[expiry] = label;
        }

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();
            var pillars = LastSensitivityDate.HasValue ? _pillarLabels.Where(x => x.Key <= LastSensitivityDate.Value).ToList() : _pillarLabels.ToList();

            foreach (var kv in pillars)
            {
                var clonedVols = new Dictionary<(DateTime expiry, double strike), double>(_vols);
                var relevantPairs = _vols.Where(v => v.Key.expiry == kv.Key);
                foreach (var pair in relevantPairs)
                {
                    clonedVols[pair.Key] = pair.Value + bumpSize;
                }
                var bumpedSurface = new SparsePointSurface(OriginDate, clonedVols, _pillarLabels)
                {
                    Name = Name,
                    AssetId = AssetId
                };

                o[kv.Value] = bumpedSurface;
            }

            return o;
        }

        public double CDF(DateTime expiry, double fwd, double strike) => throw new NotImplementedException();
        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => throw new NotImplementedException();
        public double GetForwardATMVol(double start, double end) => throw new NotImplementedException();
        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward) => throw new NotImplementedException();
        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => throw new NotImplementedException();
        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward) => throw new NotImplementedException();
        public double InverseCDF(DateTime expiry, double fwd, double p) => throw new NotImplementedException();

        public TO_SparsePointSurface GetTransportObject() => new()
        {
            OriginDate = OriginDate,
            AssetId = AssetId,
            Currency = Currency?.Ccy,
            Name = Name,
            Vols = _vols.ToDictionary(kv => $"{kv.Key.expiry:yyyy-MM-dd}~{kv.Key.strike}", kv => kv.Value),
            PointLabels = _pillarLabels.ToDictionary(kv => $"{kv.Key:yyyy-MM-dd}", kv => kv.Value),
        };
        public Dictionary<string, IVolSurface> GetATMVegaWaveyScenarios(double bumpSize, DateTime? LastSensitivityDate) => throw new NotImplementedException();
    }
}
