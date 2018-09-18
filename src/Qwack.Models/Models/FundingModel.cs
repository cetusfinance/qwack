using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Core.Models;

namespace Qwack.Models
{
    public class FundingModel : IFundingModel
    {
        private FundingModel()
        {
        }

        public FundingModel(DateTime buildDate, IrCurve[] curves)
        {
            BuildDate = buildDate;
            Curves = new Dictionary<string, IrCurve>(curves.ToDictionary(kv => kv.Name, kv => kv));
            FxMatrix = new FxMatrix();
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        public FundingModel(DateTime buildDate, Dictionary<string, IrCurve> curves)
        {
            BuildDate = buildDate;
            Curves = new Dictionary<string, IrCurve>(curves);
            FxMatrix = new FxMatrix();
            VolSurfaces = new Dictionary<string, IVolSurface>();
            SetupMappings();
        }

        private void SetupMappings()
        {
            foreach(var curve in Curves)
            {
                var key = $"{curve.Value.Currency.Ccy}é{curve.Value.CollateralSpec}";
                if (_curvesBySpec.ContainsKey(key))
                    throw new Exception($"More than one curve specifed with collateral key {key}");
                _curvesBySpec.Add(key, curve.Key);
            }
        }

        public IrCurve GetCurveByCCyAndSpec(Currency ccy, string collateralSpec)
        {
            var key = $"{ccy.Ccy}é{collateralSpec}";
            if (!_curvesBySpec.TryGetValue(key, out var curveName))
                throw new Exception($"Could not find a curve with currency {ccy.Ccy} and collateral spec {collateralSpec}");

            return Curves[curveName];
        }

        public Dictionary<string, IrCurve> Curves { get; private set; }
        public Dictionary<string, IVolSurface> VolSurfaces { get; set; }

        private Dictionary<string, string> _curvesBySpec = new Dictionary<string, string>();

        public DateTime BuildDate { get; private set; }
        public IFxMatrix FxMatrix { get; private set; }
        public string CurrentSolveCurve { get; set; }

        public void UpdateCurves(Dictionary<string, IrCurve> updateCurves) => Curves = new Dictionary<string, IrCurve>(updateCurves);

        public IFundingModel BumpCurve(string curveName, int pillarIx, double deltaBump, bool mutate)
        {
            var newModel = new FundingModel(BuildDate, Curves.Select(kv =>
            {
                if (kv.Key == curveName)
                {
                    return kv.Value.BumpRate(pillarIx, deltaBump, mutate);
                }
                else
                {
                    return kv.Value;
                }
            }).ToArray())
            {
                FxMatrix = FxMatrix
            };
            return newModel;
        }

        public IFundingModel Clone()
        {
            var returnValue = new FundingModel(BuildDate, Curves.Values.ToArray())
            {
                FxMatrix = FxMatrix,
                VolSurfaces = VolSurfaces
            };
            return returnValue;
        }

        public IFundingModel DeepClone()
        {
            var returnValue = new FundingModel(BuildDate, Curves.Values.Select(c => new IrCurve(c.PillarDates, c.GetRates(), c.BuildDate, c.Name, c.InterpolatorType, c.Currency, c.CollateralSpec)).ToArray())
            {
                VolSurfaces = VolSurfaces == null ? new Dictionary<string, IVolSurface>() : new Dictionary<string, IVolSurface>(VolSurfaces)
            };

            if (FxMatrix != null)
                returnValue.SetupFx(FxMatrix.Clone());
            return returnValue;
        }

        public double GetFxRate(DateTime settlementDate, Currency domesticCcy, Currency foreignCcy)
        { //domestic-per-foreign
            if (foreignCcy == domesticCcy) return 1.0;

            double spot;

            if (domesticCcy == FxMatrix.BaseCurrency)
            {
                spot = FxMatrix.SpotRates[foreignCcy];
            }
            else if (foreignCcy == FxMatrix.BaseCurrency)
            {
                spot = 1.0 / FxMatrix.SpotRates[domesticCcy];
            }
            else
            {
                var forToBase = GetFxRate(settlementDate, FxMatrix.BaseCurrency, foreignCcy);
                var domToBase = GetFxRate(settlementDate, FxMatrix.BaseCurrency, domesticCcy);
                return forToBase / domToBase;
            }
            var fxPair = FxMatrix.GetFxPair(domesticCcy, foreignCcy);
            var spotDate = BuildDate.AddPeriod(RollType.F, fxPair.SettlementCalendar, fxPair.SpotLag);
            var dfDom = GetDf(domesticCcy, spotDate, settlementDate);
            var dfFor = GetDf(foreignCcy, spotDate, settlementDate);

            return spot * dfDom / dfFor;
        }

        public double GetDf(string curveName, DateTime startDate, DateTime endDate)
        {
            if (!Curves.TryGetValue(curveName, out var curve))
                throw new Exception($"Curve with name {curveName} not found");

            return curve.GetDf(startDate, endDate);
        }

        public double GetDf(Currency ccy, DateTime startDate, DateTime endDate)
        {
            if (!FxMatrix.DiscountCurveMap.TryGetValue(ccy, out var curveName))
                throw new Exception($"Currency {ccy} not found in discounting map");

            if (!Curves.TryGetValue(curveName, out var curve))
                throw new Exception($"Curve with name {curveName} not found");

            return curve.GetDf(startDate, endDate);
        }

        public double GetFxAverage(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy)
        {
            return GetFxRates(fixingDates, domesticCcy, foreignCcy).Average();
        }

        public double[] GetFxRates(DateTime[] fixingDates, Currency domesticCcy, Currency foreignCcy)
        {
            if (foreignCcy == domesticCcy)
                return Enumerable.Repeat(1.0, fixingDates.Length).ToArray();
            else
            {
                var pair = FxMatrix.FxPairDefinitions.First(x => x.Domestic == domesticCcy && x.Foreign == foreignCcy);
                var settleDates = fixingDates.Select(x => x.AddPeriod(RollType.F, pair.SettlementCalendar, pair.SpotLag));
                var rates = settleDates.Select(d => GetFxRate(d, domesticCcy, foreignCcy)).ToArray();
                return rates;
            }
        }

        public void SetupFx(IFxMatrix fxMatrix) => FxMatrix = fxMatrix;
    }
}
