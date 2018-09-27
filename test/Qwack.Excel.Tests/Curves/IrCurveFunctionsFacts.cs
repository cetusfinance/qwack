using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Curves;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Curves
{
    public class IrCurveFunctionsFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void CreateDiscountCurveFromCCRates_Facts()
        {
            Assert.Equal("Could not parse interpolator type - zah", IRCurveFunctions.CreateDiscountCurveFromCCRates("pwah", "pwah", DateTime.Today, new double[] { 1.0 }, new double[] { 1.0 }, "zah", "xaf", "blah"));
            Assert.Equal("pwah¬0", IRCurveFunctions.CreateDiscountCurveFromCCRates("pwah", "pwah", DateTime.Today, new double[] { DateTime.Today.ToOADate() }, new double[] { 1.0 }, "Linear", "USD", "blah"));
        }

        [Fact]
        public void CreateDiscountCurveFromDFs_Facts()
        {
            Assert.Equal("Could not parse interpolator type - zah", IRCurveFunctions.CreateDiscountCurveFromDFs("pwah", "pwah", DateTime.Today, new double[] { 1.0 }, new double[] { 1.0 }, "zah", "xaf", "blah"));
            Assert.Equal("zwah¬0", IRCurveFunctions.CreateDiscountCurveFromDFs("zwah", "zwah", DateTime.Today, new double[] { DateTime.Today.ToOADate() }, new double[] { 1.0 }, "Linear", "USD", "blah"));
        }

        [Fact]
        public void GetDF_Facts()
        {
            Assert.Equal("IR curve mwmwah not found in cache", IRCurveFunctions.GetDF("mwmwah", DateTime.Today, DateTime.Today));
            IRCurveFunctions.CreateDiscountCurveFromDFs("mwmwah", "mwmwah", DateTime.Today, new double[] { DateTime.Today.ToOADate() }, new double[] { 1.0 }, "Linear", "USD", "blah");
            Assert.Equal(1.0, IRCurveFunctions.GetDF("mwmwah", DateTime.Today, DateTime.Today));
        }

        [Fact]
        public void GetForwardRate_Facts()
        {
            Assert.Equal("End date must be strictly greater that start date", IRCurveFunctions.GetForwardRate("mwmwahh", DateTime.Today, DateTime.Today, "Linear", "Act365F"));
            Assert.Equal("IR curve mwmwahh not found in cache", IRCurveFunctions.GetForwardRate("mwmwahh", DateTime.Today, DateTime.Today.AddDays(1), "Linear", "Act365F"));
            Assert.Equal("Could not parse rate type - blah", IRCurveFunctions.GetForwardRate("mwmwahh", DateTime.Today, DateTime.Today.AddDays(1), "blah", "Act365F"));
            Assert.Equal("Could not daycount basis - waaah", IRCurveFunctions.GetForwardRate("mwmwahh", DateTime.Today, DateTime.Today.AddDays(1), "Linear", "waaah"));

            IRCurveFunctions.CreateDiscountCurveFromDFs("mwmwahh", "mwmwahh", DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(100).ToOADate() }, new double[] { 1.0, 1.0 }, "Linear", "USD", "blah");
            
            Assert.Equal(0.0, IRCurveFunctions.GetForwardRate("mwmwahh", DateTime.Today, DateTime.Today.AddDays(1), "Linear", "Act365F"));
        }

    }
}
