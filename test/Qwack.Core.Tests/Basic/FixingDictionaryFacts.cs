using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Xunit;

namespace Qwack.Core.Tests.Basic
{
    public class FixingDictionaryFacts
    {
        [Fact]
        public void FixingDictionaryFact()
        {
            IFixingDictionary z = new FixingDictionary();
            z = new FixingDictionary(new Dictionary<DateTime, double>() { { DateTime.Today, 1.0 } });
            var zz = new FixingDictionary(z);
            var zzz = zz.Clone();

            Assert.Throws<Exception>(() => zzz.GetFixing(DateTime.MinValue));
            Assert.Equal(1.0, zzz.GetFixing(DateTime.Today));
            Assert.True(zzz.TryGetFixing(DateTime.Today, out var f));
        }

        
    }
}
