using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Options;
using Xunit;

namespace Qwack.Math.Tests.Options
{
    public class SABRFacts
    {
        [Fact]
        public void SABRParamToIVFacts()
        {
            //for  beta==1 and zero nu and rho we get a flat surface with vol==alpha
            var vol = SABR.CalcImpVol_Beta1(100, 100, 1, 0.32, 0, 0);
            Assert.Equal(0.32, vol, 10);
            vol = SABR.CalcImpVol_Beta1(100, 150, 1, 0.32, 0, 0);
            Assert.Equal(0.32, vol, 10);
            vol = SABR.CalcImpVol_Beta1(100, 50, 1, 0.32, 0, 0);
            Assert.Equal(0.32, vol, 10);

            //for positive rho should see upside vols > downside
            var volU = SABR.CalcImpVol_Beta1(100, 150, 1, 0.32, 0.5, 0.16);
            var volD = SABR.CalcImpVol_Beta1(100, 75, 1, 0.32, 0.5, 0.16);
            Assert.True(volU > volD);

            //for negative rho should see upside vols < downside
            volU = SABR.CalcImpVol_Beta1(100, 150, 1, 0.32, -0.5, 0.16);
            volD = SABR.CalcImpVol_Beta1(100, 75, 1, 0.32, -0.5, 0.16);
            Assert.True(volU < volD);

            //for larger nu should see upside wing vols higher
            volU = SABR.CalcImpVol_Beta1(100, 150, 1, 0.32, 0, 0.16);
            volD = SABR.CalcImpVol_Beta1(100, 75, 1, 0.32, 0, 0.16);
            var wingLow = volU + volD;
            volU = SABR.CalcImpVol_Beta1(100, 150, 1, 0.32, 0, 0.32);
            volD = SABR.CalcImpVol_Beta1(100, 75, 1, 0.32, 0, 0.32);
            var wingHigh = volU + volD;
            Assert.True(wingHigh > wingLow);

            //for GB parameterization, should see no sensitivity to rho and nu at ATM
            var volA = SABR.CalcImpVol_GB(100, 100, 1, 0.32, 0, 0.16);
            var volB = SABR.CalcImpVol_GB(100, 100, 1, 0.32, -0.5, 0.26);
            Assert.Equal(volA, volB, 10);

            //for hagan and berestycki, should see agreement in most cases
            var volHa = SABR.CalcImpVol_Hagan(100, 100, 1, 0.32,0.9, 0.5, 0.16);
            var volBe = SABR.CalcImpVol_Berestycki(100, 100, 1, 0.32, 0.9, 0.5, 0.16);
            Assert.Equal(volHa, volBe, 5);
            volHa = SABR.CalcImpVol_Hagan(100, 150, 1, 0.32, 0.5, 0.5, 0.16);
            volBe = SABR.CalcImpVol_Berestycki(100, 150, 1, 0.32, 0.5, 0.5, 0.16);
            Assert.Equal(volHa, volBe, 3);
        }
    }
}