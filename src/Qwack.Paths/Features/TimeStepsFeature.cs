using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Paths.Features
{
    public class TimeStepsFeature : ITimeStepsFeature, IFeatureRequiresFinish
    {
        private HashSet<DateTime> _requiredDates = new HashSet<DateTime>();
        private double[] _timeSteps;
        private double[] _times;
        private Dictionary<DateTime, int> _dateIndexes = new Dictionary<DateTime, int>();

        public int TimeStepCount => _requiredDates.Count;
        public double[] TimeSteps => _timeSteps;
        public double[] Times => _times;

        public int GetDateIndex(DateTime date) => _dateIndexes[date];
        public void AddDate(DateTime date) => _requiredDates.Add(date);

        public void AddDates(IEnumerable<DateTime> dates)
        {
            foreach (var d in dates)
            {
                _requiredDates.Add(d);
            }
        }

        public void Finish(FeatureCollection collection)
        {
            _timeSteps = new double[_requiredDates.Count];
            _times = new double[_requiredDates.Count];
            _dateIndexes = new Dictionary<DateTime, int>();
            var index = 0;
            var firstDate = default(DateTime);
            foreach (var d in _requiredDates.OrderBy(v => v))
            {
                _dateIndexes.Add(d, index);
                if (index == 0)
                {
                    firstDate = d;
                    _timeSteps[0] = 0.0;
                    _times[0] = 0.0;
                    index++;
                    continue;
                }
                _times[index] = ((d - firstDate).TotalDays / 365.0);
                _timeSteps[index] = _times[index] - _times[index - 1];
                index++;
            }
        }
    }
}
