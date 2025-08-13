using System;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Paths.Regressors
{
    public class FixedSpreadEstimator(string assetId, IAssetFxModel vanillaModel, DateTime valDate, DateTime[] averageDates, DateShifter dateShifter = null) : IForwardPriceEstimate
    {
        private double _carry;

        public bool IsComplete => true;

        public void Finish(IFeatureCollection collection) 
        {
            var curve = vanillaModel.GetPriceCurve(assetId);
            //var valDatePrompt = valDate.AddPeriod(dateShifter?.RollType ?? RollType.Following, dateShifter?.Calendar ?? curve.SpotCalendar, dateShifter?.Period ?? curve.SpotLag);
            var valDatePrompt = valDate.AddPeriod(RollType.Following, curve.SpotCalendar, curve.SpotLag);
            var averageDatesPrompts = averageDates.Select(x=>x.AddPeriod(dateShifter?.RollType ?? RollType.Following, dateShifter?.Calendar ?? curve.SpotCalendar, dateShifter?.Period ?? curve.SpotLag)).ToArray();
            _carry = curve.GetAveragePriceForDates(averageDatesPrompts) / curve.GetPriceForDate(valDatePrompt);
        }
        public double GetEstimate(double? spot) => (spot ?? 0) * _carry;
        public void Process(IPathBlock block) { }
        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection) { }
    }
}
