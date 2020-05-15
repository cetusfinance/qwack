using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Math.Interpolation;
using Qwack.Options.Calibrators;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

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

            var strikes = new double[2 * wingDeltas.Length + 1];
            var vols = new double[expiries.Length][];
            if (riskies.SelectMany(x => x).All(x => x == 0) && flies.SelectMany(x => x).All(x => x == 0))
            {
                for (var s = 0; s < wingDeltas.Length; s++)
                {
                    strikes[s] = wingDeltas[wingDeltas.Length - 1 - s];
                    strikes[strikes.Length - 1 - s] = 1.0 - wingDeltas[wingDeltas.Length - 1 - s];
                }

                for (var t =0;t<expiries.Length;t++)
                {
                    vols[t] = Enumerable.Repeat(ATMVols[t], strikes.Length).ToArray();
                }
            }
            else
            {
                var atmConstraints = ATMVols.Select(a => new ATMStraddleConstraint
                {
                    ATMVolType = atmVolType,
                    MarketVol = a
                }).ToArray();

                var needsFlip = wingDeltas.First() > wingDeltas.Last();
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

        public RiskyFlySurface(TO_RiskyFlySurface transportObject, ICurrencyProvider currencyProvider)
            :this(transportObject.OriginDate,transportObject.ATMs, transportObject.Expiries, transportObject.WingDeltas,transportObject.Riskies,
                 transportObject.Flies,transportObject.Forwards, transportObject.WingQuoteType, transportObject.AtmVolType, transportObject.StrikeInterpolatorType, 
                 transportObject.TimeInterpolatorType, transportObject.PillarLabels)
        {
            Currency = currencyProvider.GetCurrencySafe(transportObject.Currency);
            AssetId = transportObject.AssetId;
            Name = transportObject.Name;  
        }

        private int LastIx(DateTime? LastSensitivityDate)
        {
            var lastExpiry = Expiries.Length;
            if (LastSensitivityDate.HasValue)
            {
                var ix = Array.BinarySearch(Expiries, LastSensitivityDate.Value);
                ix = ix < 0 ? ~ix : ix;
                ix += 2;
                lastExpiry = System.Math.Min(ix, Expiries.Length);
            }
            return lastExpiry;
        }

        public new Dictionary<string, IVolSurface> GetATMVegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastExpiry = LastIx(LastSensitivityDate);

            for (var i = 0; i < lastExpiry; i++)
            {
                var volsBumped = (double[])ATMs.Clone();
                volsBumped[i] += bumpSize;
                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, volsBumped, Expiries, WingDeltas, Riskies, Flies, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

        public Dictionary<string, IVolSurface> GetRegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastExpiry = LastIx(LastSensitivityDate);
            var highDeltaFirst = WingDeltas.First() > WingDeltas.Last();

            for (var i = 0; i < lastExpiry; i++)
            {
                var volsBumped = (double[][])Riskies.Clone();
                if(highDeltaFirst)
                {
                    var ratios = volsBumped[i].Select(x => x / volsBumped[i][0]).ToArray();
                    volsBumped[i] = ratios.Select(r => (volsBumped[i][0] + bumpSize) * r).ToArray();
                }
                else
                {
                    var ratios = volsBumped[i].Select(x => x / volsBumped[i].Last()).ToArray();
                    volsBumped[i] = ratios.Select(r => (volsBumped[i].Last() + bumpSize) * r).ToArray();
                }
               
                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, ATMs, Expiries, WingDeltas, volsBumped, Flies, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

        public Dictionary<string, IVolSurface> GetRegaScenariosOuterWing(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastExpiry = LastIx(LastSensitivityDate);
            var highDeltaFirst = WingDeltas.First() > WingDeltas.Last();
            var outerWingIx = WingDeltas.Length - 1;

            for (var i = 0; i < lastExpiry; i++)
            {
                var volsBumped = (double[][])Riskies.Clone();

                if (highDeltaFirst)
                    volsBumped[i][outerWingIx] += bumpSize;
                else
                    volsBumped[i][0] += bumpSize;

                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, ATMs, Expiries, WingDeltas, volsBumped, Flies, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

        public Dictionary<string, IVolSurface> GetSegaScenarios(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastExpiry = LastIx(LastSensitivityDate);
            var highDeltaFirst = WingDeltas.First() > WingDeltas.Last();

            for (var i = 0; i < lastExpiry; i++)
            {
                var volsBumped = (double[][])Flies.Clone();
                if (highDeltaFirst)
                {
                    var ratios = volsBumped[i].Select(x => x / volsBumped[i][0]).ToArray();
                    volsBumped[i] = ratios.Select(r => (volsBumped[i][0] + bumpSize) * r).ToArray();
                }
                else
                {
                    var ratios = volsBumped[i].Select(x => x / volsBumped[i].Last()).ToArray();
                    volsBumped[i] = ratios.Select(r => (volsBumped[i].Last() + bumpSize) * r).ToArray();
                }

                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, ATMs, Expiries, WingDeltas, Riskies, volsBumped, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

        public Dictionary<string, IVolSurface> GetSegaScenariosOuterWing(double bumpSize, DateTime? LastSensitivityDate)
        {
            var o = new Dictionary<string, IVolSurface>();

            var lastExpiry = LastIx(LastSensitivityDate);
            var highDeltaFirst = WingDeltas.First() > WingDeltas.Last();
            var outerWingIx = WingDeltas.Length - 1;
            for (var i = 0; i < lastExpiry; i++)
            {
                var volsBumped = (double[][])Flies.Clone();
                if (highDeltaFirst)
                {
                    volsBumped[i][outerWingIx] += bumpSize;
                }
                else
                {
                    volsBumped[i][0] += bumpSize;
                }

                o.Add(PillarLabels[i], new RiskyFlySurface(OriginDate, ATMs, Expiries, WingDeltas, Riskies, volsBumped, Forwards, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, PillarLabels));
            }

            return o;
        }

        public ICube ToCube()
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "PointDate", typeof(DateTime) },
                { "PointType", typeof(string) },
                { "QuoteType", typeof(string) },
                { "PointDelta", typeof(double) },
            };
            cube.Initialize(dataTypes);

            for (var i = 0; i < Expiries.Length; i++)
            {
                var rowF = new Dictionary<string, object>
                            {
                                    { "PointDate", Expiries[i] },
                                    { "PointType", "Forward" },
                                    { "QuoteType", string.Empty },
                                    { "PointDelta", 0.5 },
                            };
                cube.AddRow(rowF, Forwards[i]);

                var rowA = new Dictionary<string, object>
                            {
                                    { "PointDate", Expiries[i] },
                                    { "PointType", "ATM" },
                                    { "QuoteType", AtmVolType.ToString() },
                                    { "PointDelta", 0.5 },
                            };
                cube.AddRow(rowA, ATMs[i]);

                for (var j = 0; j < WingDeltas.Length; j++)
                {
                    var rowRR = new Dictionary<string, object>
                            {
                                    { "PointDate", Expiries[i] },
                                    { "PointType", "RR" },
                                    { "QuoteType", WingQuoteType.ToString() },
                                    { "PointDelta", WingDeltas[j] },
                            };
                    cube.AddRow(rowRR, Riskies[i][j]);

                    var rowBF = new Dictionary<string, object>
                            {
                                    { "PointDate", Expiries[i] },
                                    { "PointType", "BF" },
                                    { "QuoteType", WingQuoteType.ToString() },
                                    { "PointDelta", WingDeltas[j] },
                            };
                    cube.AddRow(rowBF, Flies[i][j]);
                }
            }

            return cube;
        }

        public static RiskyFlySurface FromCube(ICube cube, DateTime buildDate, Interpolator1DType strikeInterpType = Interpolator1DType.GaussianKernel, Interpolator1DType timeInterpType = Interpolator1DType.LinearInVariance)
        {
            var rows = cube.GetAllRows();
            var deltas = cube.KeysForField("PointDelta")
                .Where(x => (double)x != 0.5)
                .Select(x => (double)x)
                .OrderBy(x => x)
                .ToList();
            var fwds = new Dictionary<DateTime, double>();
            var atms = new Dictionary<DateTime, double>();
            var rrs = new Dictionary<DateTime, double[]>();
            var bfs = new Dictionary<DateTime, double[]>();
            var quoteType = WingQuoteType.Simple;
            var atmType = AtmVolType.ZeroDeltaStraddle;

            foreach (var row in rows)
            {
                var r = row.ToDictionary(cube.DataTypes.Keys.ToArray());
                var d = (DateTime)r["PointDate"];
                switch ((string)r["PointType"])
                {
                    case "Forward":
                        fwds[d] = row.Value;
                        break;
                    case "ATM":
                        atms[d] = row.Value;
                        atmType = (AtmVolType)Enum.Parse(typeof(AtmVolType), (string)r["QuoteType"]);
                        break;
                    case "RR":
                        var delta = (double)r["PointDelta"];
                        if (!rrs.ContainsKey(d))
                            rrs[d] = new double[deltas.Count];
                        var dIx = deltas.IndexOf(delta);
                        rrs[d][dIx] = row.Value;
                        quoteType = (WingQuoteType)Enum.Parse(typeof(WingQuoteType), (string)r["QuoteType"]);
                        break;
                    case "BF":
                        var delta2 = (double)r["PointDelta"];
                        if (!bfs.ContainsKey(d))
                            bfs[d] = new double[deltas.Count];
                        var dIx2 = deltas.IndexOf(delta2);
                        bfs[d][dIx2] = row.Value;
                        break;
                }
            }

            var expArr = atms.OrderBy(x => x.Key).Select(x => x.Key).ToArray();
            var atmArr = atms.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            var fwdArr = fwds.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            var rrArr = rrs.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            var bfArr = bfs.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            return new RiskyFlySurface(buildDate, atmArr, expArr, deltas.ToArray(), rrArr, bfArr, fwdArr, quoteType, atmType, strikeInterpType, timeInterpType);
        }

        public object[,] DisplayQuotes()
        {
            var o = new object[Expiries.Length + 1, 2 + 2 * WingDeltas.Length];
            //headers
            o[0, 0] = "Expiry";
            o[0, 1] = "ATM";
            for(var i=0;i<WingDeltas.Length;i++)
            {
                o[0, 2 + i] = "RR~" + WingDeltas[i];
                o[0, 2 + WingDeltas.Length + i] = "BF~" + WingDeltas[i];
            }
            
            //data
            for(var i=0;i<Expiries.Length;i++)
            {
                o[1 + i, 0] = Expiries[i];
                o[1 + i, 1] = ATMs[i];
                for (var j = 0; j < WingDeltas.Length; j++)
                {
                    o[1 + i, 2 + j] = Riskies[i][j];
                    o[1 + i, 2 + WingDeltas.Length + j] = Flies[i][j];
                }
            }

            return o;
        }

        public override IVolSurface RollSurface(DateTime newOrigin)
        {
            //_suppressVarianceErrors = true;

            var newMaturities = Expiries.Where(x => x > newOrigin).ToArray();
            var newVols = new double[newMaturities.Length][];
            var newATMs = newMaturities.Select(m => GetForwardATMVol(newOrigin, m)).ToArray();
            //var newATMs = new double[newMaturities.Length];
            var newRRs = new double[newMaturities.Length][];
            var newBFs = new double[newMaturities.Length][];
            var numDropped = Expiries.Length - newMaturities.Length;

            var newFwds = Forwards.Skip(numDropped).ToArray();
            var newLabels = PillarLabels.Skip(numDropped).ToArray();

            for (var i = 0; i < newMaturities.Length; i++)
            {
                newRRs[i] = Riskies[i + numDropped];
                newBFs[i] = Flies[i + numDropped];
                //newATMs[i] = GetVolForDeltaStrike(0.5, newMaturities[i], newFwds[i]);
            }

            if (newATMs.Length == 0)
                return new ConstantVolSurface(OriginDate, 0.32)
                {
                    Name = Name,
                    AssetId = AssetId,
                    Currency = Currency,
                };

            return new RiskyFlySurface(newOrigin, newATMs, newMaturities, WingDeltas, newRRs, newBFs, newFwds, WingQuoteType, AtmVolType, StrikeInterpolatorType, TimeInterpolatorType, newLabels)
            {
                AssetId = AssetId,
                Currency = Currency,
                Name = Name,
            };
        }

        public new TO_RiskyFlySurface GetTransportObject() => new TO_RiskyFlySurface
        {
            AssetId = AssetId,
            Name = Name,
            OriginDate = OriginDate,
            ATMs = ATMs,
            AtmVolType = AtmVolType,
            Currency = Currency,
            Expiries = Expiries,
            FlatDeltaPoint = FlatDeltaPoint,
            FlatDeltaSmileInExtreme = FlatDeltaSmileInExtreme,
            OverrideSpotLag = OverrideSpotLag.ToString(),
            Riskies = new MultiDimArray<double>(Riskies),
            Flies = new MultiDimArray<double>(Flies),
            Forwards = Forwards,
            PillarLabels = PillarLabels,
            StrikeInterpolatorType = StrikeInterpolatorType,
            StrikeType = StrikeType,
            TimeBasis = TimeBasis,
            TimeInterpolatorType = TimeInterpolatorType,
            WingDeltas = WingDeltas,
            WingQuoteType = WingQuoteType
        };
    }
}
