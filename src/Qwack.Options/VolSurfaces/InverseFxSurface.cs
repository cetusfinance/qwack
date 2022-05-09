using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math;

namespace Qwack.Options.VolSurfaces
{
    public class InverseFxSurface : IATMVolSurface
    {
        private readonly ICurrencyProvider _currencyProvider;

        public InverseFxSurface(string Name, IATMVolSurface fxSurface, ICurrencyProvider currencyProvider)
        {
            FxSurface = fxSurface;
            _currencyProvider = currencyProvider;
            this.Name = Name;
        }

        public DateTime OriginDate => FxSurface.OriginDate;
        public DateTime[] Expiries => FxSurface.Expiries;
        public string Name { get; set; }
        public string InvertedPair => FxSurface.AssetId.Substring(FxSurface.AssetId.Length - 3, 3) + '/' + FxSurface.AssetId.Substring(0, 3);
        public Frequency OverrideSpotLag { get; set; }

        public Currency Currency { get => _currencyProvider.GetCurrency(FxSurface.AssetId.Substring(0, 3)); set => throw new NotImplementedException(); }
        public string AssetId { get => InvertedPair; set => throw new NotImplementedException(); }

        public IInterpolator2D LocalVolGrid { get; set; }

        public IATMVolSurface FxSurface { get; }

        public double CDF(DateTime expiry, double fwd, double strike) => 1.0 - FxSurface.CDF(expiry, 1 / fwd, 1 / strike);

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate) => FxSurface.GetATMVegaScenarios(bumpSize, LastSensitivityDate);

        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => FxSurface.GetForwardATMVol(startDate, endDate);
        public double GetForwardATMVol(double start, double end) => FxSurface.GetForwardATMVol(start, end);

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => FxSurface.GetVolForAbsoluteStrike(1.0 / strike, expiry, 1.0 / forward);
        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward) => FxSurface.GetVolForAbsoluteStrike(1.0 / strike, maturity, 1.0 / forward);

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => FxSurface.GetVolForDeltaStrike(-strike, expiry, 1.0 / forward);
        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward) => FxSurface.GetVolForDeltaStrike(-deltaStrike, maturity, 1.0 / forward);

        public double InverseCDF(DateTime expiry, double fwd, double p) => FxSurface.InverseCDF(expiry, 1 / fwd, 1 - p);

        public DateTime PillarDatesForLabel(string label) => FxSurface.PillarDatesForLabel(label);

    }
}
