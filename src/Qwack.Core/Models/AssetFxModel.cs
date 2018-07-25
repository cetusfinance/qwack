using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public class AssetFxModel
    {
        private Dictionary<string, IVolSurface> _assetVols;
        private Dictionary<string, IPriceCurve> _assetCurves;
        private readonly DateTime _buildDate;
        private readonly FundingModel _fundingModel;
        public FundingModel FundingModel => _fundingModel;

        public AssetFxModel(DateTime buildDate, FundingModel fundingModel)
        {

            _assetCurves = new Dictionary<string, IPriceCurve>();
            _assetVols = new Dictionary<string, IVolSurface>();
            _buildDate = buildDate;
            _fundingModel = fundingModel;
        }

        public void AddPriceCurve(string name, IPriceCurve curve)
        {
            _assetCurves[name] = curve;
        }

        public void AddVolSurface(string name, IVolSurface surface)
        {
            _assetVols[name] = surface;
        }
    }
}
