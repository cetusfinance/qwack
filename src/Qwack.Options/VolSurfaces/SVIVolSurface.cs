using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Options;
using Qwack.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Core.Calibrators;
using Qwack.Options.Calibrators;

namespace Qwack.Options.VolSurfaces
{
    /// <summary>
    /// A volatility surface based on the SVI parameterization
    /// </summary>
    public class SVIVolSurface : IATMVolSurface
    {
        public string Name { get; set; }
        public DateTime OriginDate { get; set; }
        public SviRawParameters[] RawParams { get; set; }
        public SviNaturalParameters[] NaturalParams { get; set; }
        public DateTime[] Expiries { get; set; }
        public double[] ExpiriesDouble { get; set; }

        public Currency Currency { get; set; }

        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearInVariance;
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;
        public string AssetId { get; set; }
        public IInterpolator2D LocalVolGrid { get; set; }

        public string[] PillarLabels { get; }
        public Frequency OverrideSpotLag { get; set; }

        private IInterpolator1D[] _interps;
        private IInterpolator1D _fwdsInterp;

        public SVIVolSurface() { }

        public SVIVolSurface(DateTime originDate, double[] ATMVols, DateTime[] expiries, double[] wingDeltas,
          double[][] riskies, double[][] flies, double[] fwds, WingQuoteType wingQuoteType, AtmVolType atmVolType,
          Interpolator1DType timeInterpType, SviType sviType=SviType.Raw, string[] pillarLabels = null):base()
        {
            if (pillarLabels == null)
                PillarLabels = expiries.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            else
                PillarLabels = pillarLabels;

            if (ATMVols.Length != expiries.Length || expiries.Length != riskies.Length || riskies.Length != flies.Length)
                throw new Exception("Inputs do not have consistent time dimensions");

            if (wingDeltas.Length != riskies[0].Length || riskies[0].Length != flies[0].Length)
                throw new Exception("Inputs do not have consistent strike dimensions");

            var atmConstraints = ATMVols.Select(a => new ATMStraddleConstraint
            {
                ATMVolType = atmVolType,
                MarketVol = a
            }).ToArray();

            var needsFlip = wingDeltas.First() > wingDeltas.Last();
            var strikes = new double[2 * wingDeltas.Length + 1];
            if (needsFlip)
            {
                for (var s = 0; s < wingDeltas.Length; s++)
                {
                    strikes[s] = wingDeltas[wingDeltas.Length - 1 - s];
                    strikes[strikes.Length - 1 - s] = 1.0 - wingDeltas[wingDeltas.Length - 1 - s];
                }
            }
            else
            {
                for (var s = 0; s < wingDeltas.Length; s++)
                {
                    strikes[s] = wingDeltas[s];
                    strikes[strikes.Length - 1 - s] = 1.0 - wingDeltas[s];
                }
            }
            strikes[wingDeltas.Length] = 0.5;

            var wingConstraints = new RRBFConstraint[expiries.Length][];
            RawParams = new SviRawParameters[expiries.Length];
            var f = new AssetSmileSolver();

            if (needsFlip)
            {
                for (var i = 0; i < wingConstraints.Length; i++)
                {
                    var offset = wingDeltas.Length - 1;
                    wingConstraints[i] = new RRBFConstraint[wingDeltas.Length];
                    for (var j = 0; j < wingConstraints[i].Length; j++)
                    {
                        wingConstraints[i][j] = new RRBFConstraint
                        {
                            Delta = wingDeltas[offset - j],
                            FlyVol = flies[i][offset - j],
                            RisykVol = riskies[i][offset - j],
                            WingQuoteType = wingQuoteType,
                        };
                    }
                    RawParams[i] = f.SolveSviRaw(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], true);
                }
            }
            else
            {
                for (var i = 0; i < wingConstraints.Length; i++)
                {
                    wingConstraints[i] = new RRBFConstraint[wingDeltas.Length];
                    for (var j = 0; j < wingConstraints[i].Length; j++)
                    {
                        wingConstraints[i][j] = new RRBFConstraint
                        {
                            Delta = wingDeltas[j],
                            FlyVol = flies[i][j],
                            RisykVol = riskies[i][j],
                            WingQuoteType = wingQuoteType,
                        };
                    }
                    RawParams[i] = f.SolveSviRaw(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], true);
                }
            }

            OriginDate = originDate;
            TimeInterpolatorType = timeInterpType;
            Expiries = expiries;
            ExpiriesDouble = expiries.Select(x => originDate.CalculateYearFraction(x,TimeBasis)).ToArray();

            _interps = new IInterpolator1D[5];
            _interps[0] = InterpolatorFactory.GetInterpolator(ExpiriesDouble, RawParams.Select(x => x.A).ToArray(), TimeInterpolatorType);
            _interps[1] = InterpolatorFactory.GetInterpolator(ExpiriesDouble, RawParams.Select(x => x.B).ToArray(), TimeInterpolatorType);
            _interps[2] = InterpolatorFactory.GetInterpolator(ExpiriesDouble, RawParams.Select(x => x.M).ToArray(), TimeInterpolatorType);
            _interps[3] = InterpolatorFactory.GetInterpolator(ExpiriesDouble, RawParams.Select(x => x.Rho).ToArray(), TimeInterpolatorType);
            _interps[4] = InterpolatorFactory.GetInterpolator(ExpiriesDouble, RawParams.Select(x => x.Sigma).ToArray(), TimeInterpolatorType);
            _fwdsInterp = InterpolatorFactory.GetInterpolator(ExpiriesDouble, fwds, Interpolator1DType.Linear);
        }

        public static double GetVolForAbsoluteStrike(double strike, double maturity, double forward, SviRawParameters rawParams) 
            => SVI.SVI_Raw_ImpliedVol(rawParams.A, rawParams.B, rawParams.Rho, strike, forward, maturity, rawParams.M, rawParams.Sigma);

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry, double forward) => GetVolForAbsoluteStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);
        public double GetVolForAbsoluteStrike(double strike, double maturity, double forward) => GetVolForAbsoluteStrike(strike, maturity, forward, new SviRawParameters
        {
            A = _interps[0].Interpolate(maturity),
            B = _interps[1].Interpolate(maturity),
            M = _interps[2].Interpolate(maturity),
            Rho = _interps[3].Interpolate(maturity),
            Sigma = _interps[4].Interpolate(maturity),
        });

        public double GetVolForDeltaStrike(double deltaStrike, double maturity, double forward) => VolUtils.GetVolForDeltaStrike(deltaStrike, maturity, forward, (k) => GetVolForAbsoluteStrike(k, maturity, forward));
        public double GetVolForDeltaStrike(double strike, DateTime expiry, double forward) => GetVolForDeltaStrike(strike, TimeBasis.CalculateYearFraction(OriginDate, expiry), forward);

        public Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate) => throw new NotImplementedException();

        public DateTime PillarDatesForLabel(string label) => throw new NotImplementedException();

        public double GetForwardATMVol(DateTime startDate, DateTime endDate) => GetForwardATMVol(TimeBasis.CalculateYearFraction(OriginDate, startDate), TimeBasis.CalculateYearFraction(OriginDate, endDate));

        public double GetForwardATMVol(double start, double end) => VolUtils.GetForwardATMVol(start, end, _fwdsInterp.Interpolate(start), _fwdsInterp.Interpolate(end), GetVolForAbsoluteStrike);

        public double InverseCDF(DateTime expiry, double fwd, double p) => VolSurfaceEx.InverseCDFex(this, OriginDate.CalculateYearFraction(expiry, DayCountBasis.Act365F), fwd, p);

        public double CDF(DateTime expiry, double fwd, double strike) => this.GenerateCDF2(100, expiry, fwd).Interpolate(strike);
    }
}
