using System;

namespace Qwack.Core.Curves.TimeProviders
{
    public interface ITimeProvider
    {
        public double GetYearFraction(DateTime start, DateTime end);
    }
}
