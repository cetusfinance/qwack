using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features.Rates
{
    public class RatesCollection : IRatesFeature
    {
        private List<IRateFeature> _ratesList = new List<IRateFeature>();
        private bool _isComplete = false;

        public bool IsComplete => _isComplete;

        public void AddRate(IRateFeature feature) => _ratesList.Add(feature);

        public void Finish(FeatureCollection collection)
        {
            var isComplete = true;
            foreach (var r in _ratesList)
            {
                if (r is IRequiresFinish finished && !finished.IsComplete)
                {
                    finished.Finish(collection);
                    isComplete = isComplete && finished.IsComplete;
                }
            }
            _isComplete = isComplete;
        }

        public IRateFeature GetRate(string name) => _ratesList.Find(f => f.RateName == name);
    }
}
