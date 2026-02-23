using System;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves.TimeProviders
{
    public interface ITimeProvider
    {
        public double GetYearFraction(DateTime start, DateTime end);
        public TO_ITimeProvider ToTransportObject();
    }
}
