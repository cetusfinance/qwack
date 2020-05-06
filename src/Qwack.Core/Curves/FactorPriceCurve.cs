using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class FactorPriceCurve : IPriceCurve
    {
        public FactorPriceCurve(IPriceCurve baseCurve, double scalingFactor)
        {
            BaseCurve = baseCurve;
            ScalingFactor = scalingFactor;
        }

        public IPriceCurve BaseCurve { get; }
        public double ScalingFactor { get; }

        public DateTime BuildDate => BaseCurve.BuildDate;
        public string Name { get => BaseCurve.Name; set => throw new NotImplementedException(); }
        public string AssetId => BaseCurve.AssetId;
        public Currency Currency { get => BaseCurve.Currency; set => throw new NotImplementedException(); }
        public int NumberOfPillars => BaseCurve.NumberOfPillars;
        public bool UnderlyingsAreForwards => BaseCurve.UnderlyingsAreForwards;
        public DateTime[] PillarDates => BaseCurve.PillarDates;
        public PriceCurveType CurveType => BaseCurve.CurveType;

        public Frequency SpotLag { get => BaseCurve.SpotLag; set => throw new NotImplementedException(); }
        public Calendar SpotCalendar { get => BaseCurve.SpotCalendar; set => throw new NotImplementedException(); }
     
        public double GetAveragePriceForDates(DateTime[] dates) => BaseCurve.GetAveragePriceForDates(dates) * ScalingFactor;

        public Dictionary<string, IPriceCurve> GetDeltaScenarios(double bumpSize, DateTime? LastDateToBump) => throw new NotImplementedException();

        public double GetPriceForDate(DateTime date) => BaseCurve.GetPriceForDate(date) * ScalingFactor;
     
        public double GetPriceForFixingDate(DateTime date) => BaseCurve.GetPriceForFixingDate(date) * ScalingFactor;

        public DateTime PillarDatesForLabel(string label) => BaseCurve.PillarDatesForLabel(label);

        public IPriceCurve RebaseDate(DateTime newAnchorDate) => throw new NotImplementedException();
    }
}
