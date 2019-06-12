using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Math;
using Qwack.Core.Basic;


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

        public ConstantVolSurface()         {        }

        public ConstantVolSurface(DateTime originDate, double volatility):base() => Build(originDate, volatility);

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

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            return new Dictionary<string, IVolSurface>
            {
                { "Flat", new ConstantVolSurface(OriginDate, Volatility + bumpSize) {Currency = Currency } }
            };
        }

        public DateTime PillarDatesForLabel(string label) => OriginDate;
    }
}
