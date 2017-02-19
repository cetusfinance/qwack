using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features
{
    public class TimeStepsFeature:ITimeStepsFeature
    {
        private HashSet<DateTime> _requiredDates =new HashSet<DateTime>();

        public int TimeStepCount => _requiredDates.Count;

        public void AddDate(DateTime date)
        {
            _requiredDates.Add(date);
        }
    }
}
