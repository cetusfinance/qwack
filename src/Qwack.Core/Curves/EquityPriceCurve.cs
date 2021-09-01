using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Core.Models;

namespace Qwack.Core.Curves
{
    public class EquityPriceCurve : IPriceCurve
    {
        private IInterpolator1D _interpDivYield;
        private ICurrencyProvider _currencyProvider;

        public PriceCurveType CurveType => PriceCurveType.Linear;
        public CommodityUnits Units { get; set; } = CommodityUnits.Unspecified;

        public bool UnderlyingsAreForwards => false; //false as we only show spot delta

        public DateTime BuildDate { get; private set; }

        public string Name { get; set; }
        public string AssetId { get; set; }

        public Frequency SpotLag { get; set; } = new Frequency("2b");
        public Calendar SpotCalendar { get; set; }

        public double Spot { get; }
        public IIrCurve IrCurve { get; }
        public DateTime SpotDate { get; }
        public double[] DivYields { get; }
        public DateTime[] DiscreteDivDates { get; }
        public double[] DiscreteDivs { get; }
        public DayCountBasis Basis { get; }
        public string[] PillarLabels { get; }
        public DateTime[] PillarDates { get; }
        public string CollateralSpec { get; set; }
        public int NumberOfPillars => PillarDates.Length;

        public Currency Currency { get; set; }

        public EquityPriceCurve(DateTime buildDate, double spot, string ccy, IIrCurve irCurve, DateTime spotDate, DateTime[] pillarDates, double[] divYields, DateTime[] discreteDivDates, double[] discreteDivs, ICurrencyProvider currencyProvider,
            DayCountBasis basis = DayCountBasis.ACT360, string[] pillarLabels = null)
        {
            _currencyProvider = currencyProvider;
            Currency = currencyProvider[ccy];
            BuildDate = buildDate;
            PillarDates = pillarDates;
            DivYields = divYields;
            DiscreteDivDates = discreteDivDates;
            DiscreteDivs = discreteDivs;
            Basis = basis;
            Spot = spot;
            IrCurve = irCurve;
            SpotDate = spotDate;

            PillarLabels = pillarLabels ?? PillarDates.Select(x => x.ToString("yyyy-MM-dd")).ToArray();
            
            Initialize();
        }

        public EquityPriceCurve(TO_EquityPriceCurve to, IFundingModel fundingModel, ICurrencyProvider currencyProvider)
            : this(to.BuildDate, to.Spot, to.Currency, fundingModel.GetCurve(to.Currency), to.SpotDate, to.PillarDates, to.DivYields, to.DiscreteDivDates, 
                  to.DiscreteDivs, currencyProvider, to.Basis, to.PillarLabels)
        {
            AssetId = to.AssetId;
            Name = to.Name;
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
        }

        private void Initialize()
        {
            var pillarsAsDoubles = PillarDates.Select(x => x.ToOADate()).ToArray();
            _interpDivYield = InterpolatorFactory.GetInterpolator(pillarsAsDoubles, DivYields, Interpolator1DType.Linear);
        }

        public double GetAveragePriceForDates(DateTime[] dates) => dates.Average(d => GetFwd(d, _interpDivYield.Interpolate(d.ToOADate())));

        public double GetPriceForDate(DateTime date) => GetFwd(date, _interpDivYield.Interpolate(date.ToOADate()));
        public double GetPriceForFixingDate(DateTime date)
        {
            var prompt = date.AddPeriod(RollType.F, SpotCalendar, SpotLag);
            return GetFwd(prompt, _interpDivYield.Interpolate(prompt.ToOADate()));
        }

        private double GetFwd(DateTime fwdDate, double divYield)
        {
            var t = SpotDate.CalculateYearFraction(fwdDate, Basis);
            var df = IrCurve.GetDf(BuildDate, fwdDate);
            var fwd = Spot / df / (1 + divYield * t);
            if(DiscreteDivDates.Any(x=>x>BuildDate && x<=fwdDate))
            {
                foreach(var d in DiscreteDivDates.Where(x => x > BuildDate && x <= fwdDate))
                {
                    var ix = Array.BinarySearch(DiscreteDivDates, d);
                    var div = DiscreteDivs[ix];
                    var dfDiv = IrCurve.GetDf(d, fwdDate);
                    fwd -= div / dfDiv;
                }
            }
            return fwd;
        }

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump)
        {
            var o = new Dictionary<string, IPriceCurve>();
            var cSpot = new EquityPriceCurve(BuildDate, Spot + bumpSize, Currency, IrCurve, SpotDate, PillarDates, DivYields, DiscreteDivDates, DiscreteDivs, _currencyProvider, Basis)
            {
                AssetId = AssetId,
                Name = Name,
                SpotCalendar = SpotCalendar,
                SpotLag = SpotLag
            };
            o.Add(AssetId, cSpot);
            return o;
        }

        public IPriceCurve RebaseDate(DateTime newAnchorDate)
        {
            var newSpotDate = newAnchorDate.SpotDate(SpotLag, SpotCalendar, SpotCalendar);
            var newSpot = GetPriceForDate(newSpotDate);
            var fwds = PillarDates.Select(p => GetPriceForDate(p)).ToArray();
            var times = PillarDates.Select(p => newSpotDate.CalculateYearFraction(p, Basis)).ToArray();
            var newCtgos = fwds.Select((f, ix) => times[ix] == 0 ? 0.0 : (f / newSpot - 1.0) / times[ix]).ToArray();

            var o = new EquityPriceCurve(newAnchorDate, newSpot, Currency, IrCurve, newSpotDate, PillarDates, DivYields, DiscreteDivDates, DiscreteDivs, _currencyProvider, Basis)
            {
                AssetId = AssetId,
                Name = Name,
                SpotCalendar = SpotCalendar,
                SpotLag = SpotLag
            };
            return o;
        }

        public DateTime PillarDatesForLabel(string label)
        {
            if (label == "Spot" || label == AssetId)
                return SpotDate;
            var labelIx = Array.IndexOf(PillarLabels, label);
            return PillarDates[labelIx];
        }
    }
}
