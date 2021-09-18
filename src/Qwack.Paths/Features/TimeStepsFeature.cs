using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;
using Qwack.Utils.Parallel;

namespace Qwack.Paths.Features
{
    public class TimeStepsFeature : ITimeStepsFeature, IRequiresFinish
    {
        private readonly object _locker = new();

        private readonly ConcurrentHashSet<DateTime> _requiredDates = new();
        private double[] _timeSteps;
        private double[] _timeStepsSqrt;
        private double[] _times;
        private bool _isComplete;
        private Dictionary<DateTime, int> _dateIndexes = new();

        public int TimeStepCount => Dates?.Length ?? _requiredDates.Count;
        public double[] TimeSteps => _timeSteps;
        public double[] TimeStepsSqrt => _timeStepsSqrt;
        public double[] Times => _times;
        public DateTime[] Dates { get; private set; }

        public bool IsComplete => _isComplete;

        public int GetDateIndex(DateTime date) => _dateIndexes.TryGetValue(date, out var d) ? d : -1;
        public void AddDate(DateTime date) => _requiredDates.Add(date);


        public void AddDates(IEnumerable<DateTime> dates)
        {
            foreach (var d in dates)
            {
                _requiredDates.Add(d);
            }
        }

        public void Finish(IFeatureCollection collection)
        {
            Dates = _requiredDates.ToArray().OrderBy(v => v).ToArray();
            _timeSteps = new double[_requiredDates.Count];
            _timeStepsSqrt = new double[_requiredDates.Count];
            _times = new double[_requiredDates.Count];
            _dateIndexes = new Dictionary<DateTime, int>();
            var index = 0;
            var firstDate = default(DateTime);
            foreach (var d in Dates)
            {
                _dateIndexes.Add(d, index);
                if (index == 0)
                {
                    firstDate = d;
                    _timeSteps[0] = 0.0;
                    _times[0] = 0.0;
                    _timeStepsSqrt[0] = 0;
                    index++;
                    continue;
                }
                _times[index] = ((d - firstDate).TotalDays / 365.0);
                _timeSteps[index] = _times[index] - _times[index - 1];
                _timeStepsSqrt[index] = System.Math.Sqrt(_timeSteps[index]);
                index++;
            }
            _isComplete = true;
        }
    }
}
