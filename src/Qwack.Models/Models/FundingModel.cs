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
        }

        public FundingModel(DateTime buildDate, Dictionary<string, IrCurve> curves)
        {
            BuildDate = buildDate;
            Curves = new Dictionary<string, IrCurve>(curves);
        }

        public Dictionary<string, IrCurve> Curves { get; private set; }
        public Dictionary<string, IVolSurface> VolSurfaces { get; set; }

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
            var returnValue = new FundingModel(BuildDate, Curves.Values.Select(c => new IrCurve(c.PillarDates, c.GetRates(), c.BuildDate, c.Name, c.InterpolatorType)).ToArray())
            {
                VolSurfaces = new Dictionary<string, IVolSurface>(VolSurfaces)
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
            var dfDom = Curves[FxMatrix.DiscountCurveMap[domesticCcy]].GetDf(spotDate, settlementDate);
            var dfFor = Curves[FxMatrix.DiscountCurveMap[foreignCcy]].GetDf(spotDate, settlementDate);

            return spot * dfDom / dfFor;
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
