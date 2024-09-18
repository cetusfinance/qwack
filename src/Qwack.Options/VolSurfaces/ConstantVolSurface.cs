using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Options.VolSurfaces
{
    /// <summary>
    /// A volatility which returns a single constant number for all strikes and expiries
    /// </summary>
    public class ConstantVolSurface : IATMVolSurface
    {
        public DateTime OriginDate { get; set; }
        public double Volatility { get; set; }
        public string Name { get; set; }

        public Currency Currency { get; set; }
        public string AssetId { get; set; }

        public IInterpolator2D LocalVolGrid { get; set; }

        public DateTime[] Expiries => new[] { OriginDate };
        public Frequency OverrideSpotLag { get; set; }
        public ConstantVolSurface() { }

        public ConstantVolSurface(DateTime originDate, double volatility) : base() => Build(originDate, volatility);

        public ConstantVolSurface(TO_ConstantVolSurface transportObject, ICurrencyProvider currencyProvider)
            : this(transportObject.OriginDate, transportObject.Volatility)
        {
            if (transportObject.Currency != null)
                Currency = currencyProvider.GetCurrency(transportObject.Currency);
            AssetId = transportObject.AssetId;
            Name = transportObject.Name;
        }

        public void Build(DateTime originDate, double volatility)
        {
            OriginDate = originDate;
            Volatility = volatility;
        }

        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward) => Volatility;

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => Volatility;

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward) => Volatility;

        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => Volatility;

        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => Volatility;

        public double GetForwardATMVol(double start, double end) => Volatility;

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate) => new()
        {
                { "Flat", new ConstantVolSurface(OriginDate, Volatility + bumpSize) {Currency = Currency, AssetId=AssetId, Name=Name } }
            };

        public Dictionary<string, IVolSurface> GetATMVegaWaveyScenarios(double bumpSize, DateTime? LastSensitivityDate) => new()
        {
                { "Flat", new ConstantVolSurface(OriginDate, Volatility + bumpSize) {Currency = Currency, AssetId=AssetId, Name=Name } }
            };

        public DateTime PillarDatesForLabel(string label) => OriginDate;

        public double InverseCDF(DateTime expiry, double fwd, double p) => VolSurfaceEx.InverseCDFex(this, OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), fwd, p);
        public double CDF(DateTime expiry, double fwd, double strike) => this.GenerateCDF2(100, expiry, fwd).Interpolate(strike);

        public double[] Returns { get; set; }

        public TO_ConstantVolSurface GetTransportObject() => new()
        {
            AssetId = AssetId,
            Name = Name,
            OriginDate = OriginDate,
            Currency = Currency?.Ccy,
            Volatility = Volatility
        };
    }
}
