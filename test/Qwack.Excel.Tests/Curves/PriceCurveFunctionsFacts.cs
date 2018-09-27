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
using Qwack.Math;

namespace Qwack.Excel.Tests.Curves
{
    public class PriceCurveFunctionsFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);

        [Fact]
        public void CreatePriceCurve_Facts()
        {
            Assert.Equal("Could not parse price curve type - zah", PriceCurveFunctions.CreatePriceCurve("pwah", "pwahAid", DateTime.Today, new double[] { 1.0 }, new double[] { 1.0 }, "zah", "xaf", "blah", "gwah", "zzz", "zzzCal"));
            Assert.Equal("pwah¬0", PriceCurveFunctions.CreatePriceCurve("pwah", "pwahAid", DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(10).ToOADate() }, new double[] { 1.0, 1.1 }, "ICE", ExcelDna.Integration.ExcelMissing.Value, "USD", "gwah", "2b", "USD"));
            Assert.Equal("pwah1¬0", PriceCurveFunctions.CreatePriceCurve("pwah1", "pwahAid", DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(10).ToOADate() }, new double[] { 1.0, 1.1 }, "ICE", new object[,] { { "l1", "l2" } }, "USD", "gwah", "2b", "USD"));
            Assert.Equal("pwah2¬0", PriceCurveFunctions.CreatePriceCurve("pwah2", null, DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(10).ToOADate() }, new double[] { 1.0, 1.1 }, "ICE", new object[,] { { "l1", "l2" } }, "USD", "gwah", "2b", "USD"));
        }

        [Fact]
        public void CreateContangoPriceCurve_Facts()
        {
            Assert.Equal("glah¬0", PriceCurveFunctions.CreateContangoPriceCurve("glah", "pwahAid", DateTime.Today, DateTime.Today.AddDays(2), 100.0, new double[] { DateTime.Today.AddDays(10).ToOADate(), DateTime.Today.AddDays(20).ToOADate() }, new double[] { 0.05, 0.06 }, ExcelDna.Integration.ExcelMissing.Value));
            Assert.Equal("glah1¬0", PriceCurveFunctions.CreateContangoPriceCurve("glah1", "pwahAid", DateTime.Today, DateTime.Today.AddDays(2), 100.0, new double[] { DateTime.Today.AddDays(10).ToOADate(), DateTime.Today.AddDays(20).ToOADate() }, new double[] { 0.05, 0.06 }, new object[,] { { "l1", "l2" } }));
        }

        [Fact]
        public void CreateSparsePriceCurve_Facts()
        {
            Assert.Equal("Could not parse price curve type - zah", PriceCurveFunctions.CreateSparsePriceCurve("pwah", "pwahAid", DateTime.Today, new double[] { 1.0 }, new double[] { 1.0 }, "zah"));
            Assert.Equal("pwahg¬0", PriceCurveFunctions.CreateSparsePriceCurve("pwahg", "pwahAid", DateTime.Today, new double[] { 1.0 }, new double[] { 1.0 }, "Coal"));
        }

        [Fact]
        public void GetPrice_Facts()
        {
            Assert.Equal("Price curve pwahhh not found in cache", PriceCurveFunctions.GetPrice("pwahhh", DateTime.Today));
            PriceCurveFunctions.CreatePriceCurve("pwahhh", "pwahAid", DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(10).ToOADate() }, new double[] { 1.0, 1.1 }, "ICE", ExcelDna.Integration.ExcelMissing.Value, "USD", "gwah", "2b", "USD");
            Assert.Equal(1.0, PriceCurveFunctions.GetPrice("pwahhh", DateTime.Today));
        }

        [Fact]
        public void GetAveragePrice_Facts()
        {
            Assert.Equal("Price curve pwahhhff not found in cache", PriceCurveFunctions.GetAveragePrice("pwahhhff", new[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(1).ToOADate() }));
            PriceCurveFunctions.CreatePriceCurve("pwahhhff", "pwahAid", DateTime.Today, new double[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(10).ToOADate() }, new double[] { 1.0, 1.1 }, "ICE", ExcelDna.Integration.ExcelMissing.Value, "USD", "gwah", "2b", "USD");
            Assert.Equal(1.05, PriceCurveFunctions.GetAveragePrice("pwahhhff", new[] { DateTime.Today.ToOADate(), DateTime.Today.AddDays(1).ToOADate() }));
        }

        [Fact]
        public void CreateFixingDictionary_Facts()
        {
            Assert.Equal("Unknown fixing dictionary type creugh", PriceCurveFunctions.CreateFixingDictionary("bleugh", "bleugh", new object[,] { { DateTime.Today, 1000.0 } }, "creugh"));
            Assert.Equal("bleugh¬0", PriceCurveFunctions.CreateFixingDictionary("bleugh", "bleugh", new object[,] { { DateTime.Today, 1000.0 } }, "FX"));
        }

        [Fact]
        public void CreateFixingDictionaryFromVectors_Facts()
        {
            Assert.Equal("Unknown fixing dictionary type creugh", PriceCurveFunctions.CreateFixingDictionaryFromVectors("bleugh2", "bleugh", new double[] { DateTime.Today.ToOADate() }, new double[] { 1000.0 }, "creugh"));
            Assert.Equal("Fixings and FixingDates must be of same length", PriceCurveFunctions.CreateFixingDictionaryFromVectors("bleugh2", "bleugh", new double[] { DateTime.Today.ToOADate() }, new double[] { 1000.0, 1001.0 }, ExcelDna.Integration.ExcelMissing.Value));
            Assert.Equal("bleugh2¬0", PriceCurveFunctions.CreateFixingDictionaryFromVectors("bleugh2", "bleugh", new double[] { DateTime.Today.ToOADate() }, new double[] { 1000.0 }, "Asset"));
        }

        [Fact]
        public void DisplayFixingDictionary_Facts()
        {
            Assert.Equal("Fixing dictionary not found with name creughh", PriceCurveFunctions.DisplayFixingDictionary("creughh", "bleugh", true));

            PriceCurveFunctions.CreateFixingDictionary("creughh", "bleugh", new object[,] { { DateTime.Today, 1000.0 } }, "FX");

            Assert.Equal($"Fixing not found for date {DateTime.Today.AddDays(1).ToOADate()}", PriceCurveFunctions.DisplayFixingDictionary("creughh", DateTime.Today.AddDays(1).ToOADate(), true));
            Assert.Equal(1000.0, PriceCurveFunctions.DisplayFixingDictionary("creughh", DateTime.Today.ToOADate(), true));

            var output = (object[,]) PriceCurveFunctions.DisplayFixingDictionary("creughh", ExcelDna.Integration.ExcelMissing.Value, true);
            Assert.Equal(DateTime.Today, output[0, 0]);
            Assert.Equal(1000.0, output[0, 1]);

            output = (object[,])PriceCurveFunctions.DisplayFixingDictionary("creughh", ExcelDna.Integration.ExcelMissing.Value, false);
            Assert.Equal(DateTime.Today, output[0, 0]);
            Assert.Equal(1000.0, output[0, 1]);
        }

        [Fact]
        public void CreateCorrelationMatrix_Facts()
        {
            Assert.Equal("Inconsistent dimensions between labels and data", PriceCurveFunctions.CreateCorrelationMatrix("raaar", new[] { "l1", "l2" }, new[] { "l3" }, new double[,] { { 1.0, 1.0, 1.0 } }));
            Assert.Equal("Inconsistent dimensions between labels and data", PriceCurveFunctions.CreateCorrelationMatrix("raaar", new[] { "l1", "l2" }, new[] { "l3" }, new double[,] { { 1.0, 1.0 } }));
            Assert.Equal("raaar¬0", PriceCurveFunctions.CreateCorrelationMatrix("raaar", new[] { "l1", "l2" }, new[] { "l3" }, new double[,] { { 1.0 },{ 1.0 } }));
        }

        [Fact]
        public void BendCurve_Facts()
        {
            var input = new double[] { 1.0, 2.0, 3.0 };
            var sparse = new object[] { 1.0, ExcelDna.Integration.ExcelMissing.Value, 4.0 };
            var sparse2 = new double?[] { 1.0, null, 4.0 };
            var z= PriceCurveFunctions.BendCurve(input, sparse);
            Assert.True(Enumerable.SequenceEqual(CurveBender.Bend(input,sparse2), (double[])PriceCurveFunctions.BendCurve(input, sparse)));
            
        }
    }
}
