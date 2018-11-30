using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Descriptors;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Calibrators;
using System.Linq;

namespace Qwack.Core.Curves
{
    public class BasisPriceCurve : IPriceCurve
    {
        private readonly ICurrencyProvider _ccyProvider;

        public IPriceCurve BaseCurve { get; private set; }
        public IPriceCurve Curve { get; private set; }

        public BasisPriceCurve(List<AsianBasisSwap> instruments, List<DateTime> pillars, IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, PriceCurveType curveType, ICurrencyProvider ccyProvider, List<string> pillarLabels = null)
        {
            Instruments = instruments;
            Pillars = pillars;
            DiscountCurve = discountCurve;
            BaseCurve = baseCurve;
            CurveType = curveType;
            _ccyProvider = ccyProvider;
            BuildDate = buildDate;
            PillarLabels = pillarLabels ?? pillars.Select(x => x.ToString("yyyy-MM-dd")).ToList();

            var solver = new NewtonRaphsonAssetBasisCurveSolver(_ccyProvider);
            Curve = solver.SolveCurve(Instruments, Pillars, DiscountCurve, BaseCurve, BuildDate, CurveType);
        }

        public DateTime BuildDate { get; }
        public string Name { get; set; }

        public string AssetId { get; set; }
        public Currency Currency { get; set; }

        public int NumberOfPillars => Curve.NumberOfPillars;
        public bool UnderlyingsAreForwards => Curve.UnderlyingsAreForwards;
        public DateTime[] PillarDates => Curve.PillarDates;
        public PriceCurveType CurveType { get; }

        public List<MarketDataDescriptor> Descriptors => throw new NotImplementedException();
        public List<MarketDataDescriptor> Dependencies => throw new NotImplementedException();
        public Dictionary<MarketDataDescriptor, object> DependentReferences => throw new NotImplementedException();

        public List<AsianBasisSwap> Instruments { get; }
        public List<DateTime> Pillars { get; }
        public List<string> PillarLabels { get; }
        public IIrCurve DiscountCurve { get; }

        public double GetAveragePriceForDates(DateTime[] dates) => Curve.GetAveragePriceForDates(dates);

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();

            var insToBump = LastDateToBump.HasValue ? 
                Instruments.Where(x => x.LastSensitivityDate < LastDateToBump.Value.AddMonths(1)).ToList() : 
                Instruments.ToList();
            
            for (var i = 0; i < insToBump.Count; i++)
            {
                var insListClone = Instruments.Select(x => (AsianBasisSwap)x.Clone()).ToList();
                insListClone[i] = (AsianBasisSwap)insListClone[i].SetStrike(insListClone[i].Strike + bumpSize);

                var bumpedCurve = new BasisPriceCurve(insListClone, Pillars, DiscountCurve, BaseCurve, BuildDate, CurveType, _ccyProvider) { Currency = Currency, AssetId = AssetId, Name = Name };

                o.Add(PillarLabels[i], bumpedCurve);
            }

            return o;
        }

        public double GetPriceForDate(DateTime date) => Curve.GetPriceForDate(date);
        public DateTime PillarDatesForLabel(string label) => Curve.PillarDatesForLabel(label);
        public IPriceCurve RebaseDate(DateTime newAnchorDate) => new BasisPriceCurve(Instruments.Select(x => (AsianBasisSwap)x.Clone()).ToList(), Pillars, DiscountCurve, Curve.RebaseDate(newAnchorDate), BuildDate, CurveType, _ccyProvider) { Currency = Currency, AssetId = AssetId, Name = Name };
        
        public BasisPriceCurve Clone() => new BasisPriceCurve(Instruments.Select(x => (AsianBasisSwap)x.Clone()).ToList(), Pillars, DiscountCurve, BaseCurve, BuildDate, CurveType, _ccyProvider) { Currency = Currency, AssetId = AssetId, Name = Name };
        public BasisPriceCurve ReCalibrate(IPriceCurve NewBaseCurve) => new BasisPriceCurve(Instruments.Select(x => (AsianBasisSwap)x.Clone()).ToList(), Pillars, DiscountCurve, NewBaseCurve, BuildDate, CurveType, _ccyProvider) { Currency = Currency, AssetId = AssetId, Name = Name };
    }
}
