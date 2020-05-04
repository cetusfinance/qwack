using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Xunit;

namespace Qwack.Core.Tests.Basic
{
    public class FxPairFacts
    {
        [Fact]
        public void FxPairFact()
        {
            var z = new FxPair
            {
                Domestic = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                Foreign = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR"),
                PrimaryCalendar = TestProviderHelper.CalendarProvider.Collection["NYC"],
                SpotLag = new Dates.Frequency("2d")
            };

            var zz = new FxPair
            {
                Domestic = TestProviderHelper.CurrencyProvider.GetCurrency("ZAR"),
                Foreign = TestProviderHelper.CurrencyProvider.GetCurrency("USD"),
                PrimaryCalendar = TestProviderHelper.CalendarProvider.Collection["NYC"],
                SpotLag = new Dates.Frequency("2d")
            };

            Assert.False(z.Equals((object)"woooo"));
            Assert.False(z.Equals(zz));
            Assert.True(z.Equals(z));

            Assert.Equal(z.GetHashCode(), z.GetHashCode());
        }

        
    }
}
