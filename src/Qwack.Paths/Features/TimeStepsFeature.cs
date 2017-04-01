using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Paths.Features
{
    public class TimeStepsFeature:ITimeStepsFeature, IFeatureRequiresFinish
    {
        private HashSet<DateTime> _requiredDates =new HashSet<DateTime>();
        private double[] _timeSteps;
        public int TimeStepCount => _requiredDates.Count;
        public double[] TimeSteps => _timeSteps;

        public void AddDate(DateTime date)
        {
            _requiredDates.Add(date);
        }

        public void Finish(FeatureCollection collection)
        {
            _timeSteps = new double[_requiredDates.Count];
            var index = 0;
            DateTime firstDate = default(DateTime);
            foreach(var d in _requiredDates.OrderBy(v => v))
            {
                if(index ==0)
                {
                    firstDate = d;
                    _timeSteps[0] = 0.0;
                    index++;
                    continue;
                }
                _timeSteps[index] = (d - firstDate).TotalDays / 365.0;
                index++;
            }
        }
    }
}
