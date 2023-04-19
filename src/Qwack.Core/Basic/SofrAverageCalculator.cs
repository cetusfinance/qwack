using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;

namespace Qwack.Core.Basic
{
    public static class SofrAverageCalculator
    {
        //https://www.cmegroup.com/content/dam/cmegroup/rulebook/CME/IV/400/460.pdf
        public static double ComputeAverage(List<DateTime> dates, Dictionary<DateTime, double> fixings, IIrCurve forecastCurve, DateTime? valDate = null)
        {
            var vd = valDate ?? forecastCurve.BuildDate;
            var sortedDates = dates.OrderBy(x => x).ToList();
            var daysInPeriod = (sortedDates.Last() - sortedDates.First()).TotalDays;
            var sum = 1.0;
            
            for(var i=0;i<sortedDates.Count; i++)
            {
                var date = sortedDates[i];
                var rate = 0.0;

                if (date < vd)
                {
                    if (!fixings.TryGetValue(date, out rate))
                    {
                        var recentDate = sortedDates.Where(x=>x<date).Max();
                        rate = fixings[recentDate];
                    }
                }
                else if (date == vd)
                {
                    if (!fixings.TryGetValue(date, out rate))
                    {
                        rate = forecastCurve.GetRate(date);
                    }
                }
                else
                    rate = forecastCurve.GetRate(date);

                var d = i == sortedDates.Count - 1 ? 1 : (sortedDates[i + 1] - date).TotalDays;

                sum *= 1 + d / 360.0 * rate;
            }

            sum -= 1.0;
            sum *= 360.0 / daysInPeriod;

            return sum;
        }
    }
}
