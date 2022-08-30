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

        [Fact]
 
        public void xccyTests()
        {
            var cleanPrice = 101.50 / 100;
            var couponRate = 0.05875;
            var couponsPerYear = 2.0;
            var faceValue = 100;
            var t = 2.75;
            var tNext = 0.25;
            var fxRates = (double t) => Exp(t * 0.02);
            var ytm = BondUtils.YieldToMaturity(couponRate * faceValue, faceValue, cleanPrice*100, t);
            var ytmX = BondUtils.YtmInBase(couponRate, faceValue, couponsPerYear, t, tNext, fxRates, cleanPrice);

            Assert.Equal(0.083453953662142838, ytmX, 3);

            couponsPerYear = 1.0;
            tNext = 0.75;
            ytm = BondUtils.YieldToMaturity(couponRate * faceValue, faceValue, cleanPrice * 100, t);
            ytmX = BondUtils.YtmInBase(couponRate, faceValue, couponsPerYear, t, tNext, fxRates, cleanPrice);

            Assert.Equal(0.07988081304417495, ytmX, 3);
        }
    }
}
