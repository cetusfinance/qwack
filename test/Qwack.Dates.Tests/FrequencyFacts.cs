using System;
using System.Collections.Generic;
using Qwack.Providers.Json;
using Qwack.Transport.BasicTypes;
using Xunit;

namespace Qwack.Dates.Tests
{
    public class FrequencyFacts
    {
  
        [Fact]
        public void CheckEquivalence()
        {
            Assert.Equal(new Frequency("2m"), new Frequency(2, DatePeriodType.Month));
            Assert.Equal(new Frequency("1d"), new Frequency(1, DatePeriodType.Day));
            Assert.Equal(new Frequency("7b"), new Frequency(7, DatePeriodType.BusinessDay));
            Assert.Equal(new Frequency("2w"), new Frequency(2, DatePeriodType.Week));
            Assert.Equal(new Frequency("3y"), new Frequency(3, DatePeriodType.Year));

            Assert.True(new Frequency("3y") != new Frequency(3, DatePeriodType.D));
            Assert.True(new Frequency("3y") != new Frequency(1, DatePeriodType.Y));

            Assert.Equal(new Frequency("3y").GetHashCode(), new Frequency(3, DatePeriodType.Year).GetHashCode());

            Assert.Equal(3.Weeks(), new Frequency("3w"));
            Assert.Equal(3.Day(), new Frequency("3d"));
            Assert.Equal(3.Years(), new Frequency("3y"));
            Assert.Equal(3.Bd(), new Frequency("3b"));
            Assert.Equal(3.Months(), new Frequency("3m"));

            Assert.Equal("3M", 3.Months().ToString());
        }

   
    }
}
