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
        public void YtcFromPriceFacts()
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

        [Fact]
        public void YtmRoundTrip()
        {
            var cleanPrice = 1175.0;
            var couponRate = 0.1;
            var faceValue = 1000.0;
            var t = 5.0;

            var ytm = BondUtils.YieldToMaturity(couponRate, faceValue, cleanPrice, t);
            var price = BondUtils.PriceFromYtm(couponRate, faceValue, ytm, t);

            Assert.Equal(cleanPrice, price, 8);
        }

        [Fact]
        //https://www.investopedia.com/terms/d/duration.asp
        public void MacaulayTests()
        {
            var cleanPrice = 110.83;
            var couponRate = 0.1;
            var couponsPerYear = 2.0;
            var faceValue = 100;
            var ytm = 0.06;
            var t = 3.0;
            var tNext = 0.5;

            var mac = BondUtils.MacaulayDuration(couponRate, faceValue, ytm, couponsPerYear, t, tNext, cleanPrice);
            
            Assert.Equal(2.684, mac, 3);
        }
    }
}
