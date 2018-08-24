using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;

namespace Qwack.Models
{
    public class AssetFxModel : IAssetFxModel
    {
        private readonly Dictionary<string, IVolSurface> _assetVols;
        private readonly Dictionary<string, IPriceCurve> _assetCurves;
        private readonly Dictionary<string, IDictionary<DateTime, double>> _fixings;
        private readonly DateTime _buildDate;
        private readonly IFundingModel _fundingModel;

        public IFundingModel FundingModel => _fundingModel;
        public DateTime BuildDate => _buildDate;

        public object TurnbullWakeman { get; private set; }

        public AssetFxModel(DateTime buildDate, IFundingModel fundingModel)
        {
            _assetCurves = new Dictionary<string, IPriceCurve>();
            _assetVols = new Dictionary<string, IVolSurface>();
            _fixings = new Dictionary<string, IDictionary<DateTime, double>>();
            _buildDate = buildDate;
            _fundingModel = fundingModel;
        }

        public void AddPriceCurve(string name, IPriceCurve curve) => _assetCurves[name] = curve;

        public void AddVolSurface(string name, IVolSurface surface) => _assetVols[name] = surface;

        public IPriceCurve GetPriceCurve(string name) => _assetCurves[name];

        public IVolSurface GetVolSurface(string name) => _assetVols[name];

        public void AddFixingDictionary(string name, IDictionary<DateTime, double> fixings) => _fixings[name] = fixings;

        public IDictionary<DateTime, double> GetFixingDictionary(string name) => _fixings[name];

        public bool TryGetFixingDictionary(string name, out IDictionary<DateTime, double> fixings) => _fixings.TryGetValue(name, out fixings);
    }
}
