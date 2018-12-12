using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Calibrators;
using Qwack.Math.Interpolation;

namespace Qwack.Options.VolSurfaces
{
    public class RiskyFlySurface : GridVolSurface
    {
        public double[][] Riskies { get; }
        public double[][] Flies { get; }
        public double[] ATMs { get; }
        public double[] WingDeltas { get; }
        public double[] Forwards { get; }
        public WingQuoteType WingQuoteType { get; }
        public AtmVolType AtmVolType { get; }


        public RiskyFlySurface(DateTime originDate, double[] ATMVols, DateTime[] expiries, double[] wingDeltas, 
            double[][] riskies, double[][] flies, double[] fwds, WingQuoteType wingQuoteType, AtmVolType atmVolType, 
            Interpolator1DType strikeInterpType, Interpolator1DType timeInterpType, string[] pillarLabels = null)
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
            var vols = new double[expiries.Length][];
            var f = new NewtonRaphsonAssetSmileSolver();

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
                            Delta = wingDeltas[offset-j],
                            FlyVol = flies[i][offset-j],
                            RisykVol = riskies[i][offset-j],
                            WingQuoteType = wingQuoteType,
                        };
                    }
                    vols[i] = f.Solve(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], strikes, strikeInterpType);
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
                    vols[i] = f.Solve(atmConstraints[i], wingConstraints[i], originDate, expiries[i], fwds[i], strikes, strikeInterpType);
                }
            }

            StrikeInterpolatorType = strikeInterpType;
            TimeInterpolatorType = timeInterpType;
            Expiries = expiries;
            ExpiriesDouble = expiries.Select(x => x.ToOADate()).ToArray();
            Strikes = strikes;
            StrikeType = StrikeType.ForwardDelta;

            ATMs = ATMVols;
            Riskies = riskies;
            Flies = flies;
            WingDeltas = wingDeltas;
            Forwards = fwds;
            WingQuoteType = wingQuoteType;
            AtmVolType = atmVolType;

            base.Build(originDate, strikes, expiries, vols);
        }

        public new Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            for (var i = 0; i < Expiries.Length; i++)
            {
                var volsBumped = (double[])ATMs.Clone();
                volsBumped[i] += bumpSize;
                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, volsBumped, Expiries, WingDeltas, Riskies, Flies, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

    }
}
