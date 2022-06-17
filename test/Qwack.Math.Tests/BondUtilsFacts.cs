using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Qwack.Math;
using static System.Math;

namespace Qwack.Math.Tests
{
    public class BondUtilsFacts
    {
        [Fact]
        //https://www.investopedia.com/terms/y/yieldtocall.asp
        public void BondUtilFacts()
        {
            var cleanPrice = 1175.0;
            var couponRate = 0.1;
            var faceValue = 1000.0;
            var callPrice = 1100.0;
            var tCall = 5.0;

            var ytc = BondUtils.YtcFromPrice(couponRate, callPrice/faceValue, tCall, cleanPrice/faceValue);
            //var ytc = BondUtils.YtcFromPrice(couponRate * faceValue, callPrice , tCall, cleanPrice );

            Assert.Equal(0.0743, ytc, 4);
        }
    }
}
