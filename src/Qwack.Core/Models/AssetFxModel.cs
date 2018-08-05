using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Asset;

namespace Qwack.Core.Models
{
    public class AssetFxModel
    {
        private readonly Dictionary<string, IVolSurface> _assetVols;
        private readonly Dictionary<string, IPriceCurve> _assetCurves;
        private readonly DateTime _buildDate;
        private readonly FundingModel _fundingModel;

        public FundingModel FundingModel => _fundingModel;
        public DateTime BuildDate => _buildDate;

        public object TurnbullWakeman { get; private set; }

        public AssetFxModel(DateTime buildDate, FundingModel fundingModel)
        {

            _assetCurves = new Dictionary<string, IPriceCurve>();
            _assetVols = new Dictionary<string, IVolSurface>();
            _buildDate = buildDate;
            _fundingModel = fundingModel;
        }

        public void AddPriceCurve(string name, IPriceCurve curve) => _assetCurves[name] = curve;

        public void AddVolSurface(string name, IVolSurface surface) => _assetVols[name] = surface;

        public IPriceCurve GetPriceCurve(string name) => _assetCurves[name];

        public IVolSurface GetVolSurface(string name) => _assetVols[name];
    }
}
