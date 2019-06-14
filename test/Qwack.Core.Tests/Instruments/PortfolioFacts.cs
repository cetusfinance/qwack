using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Curves;
using System.Linq;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using Qwack.Models;
using Qwack.Dates;
using static System.Math;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Providers.Json;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;

namespace Qwack.Core.Tests.Instruments
{
    public class PortfolioFacts
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonCcyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly ICurrencyProvider CcyProvider = new CurrenciesFromJson(CalendarProvider, JsonCcyPath, TestProviderHelper.LoggerFactory);

        [Fact]
        public void Portfolio()
        {
            var bd = DateTime.Parse("2018-09-13");
            var swp = AssetProductFactory.CreateTermAsianSwap(bd, bd.AddYears(1), 100, "AssetX", CalendarProvider.Collection["USD"], bd.AddYears(2), CcyProvider.GetCurrency("USD"));

            var pf = new Portfolio() { Instruments = new List<IInstrument>() };
            Assert.Empty(PortfolioEx.AssetIds(pf));

            pf.Instruments = new List<IInstrument> { swp };

            Assert.Equal(swp.LastSensitivityDate, pf.LastSensitivityDate);
            Assert.Equal("AssetX", PortfolioEx.AssetIds(pf).First());
            Assert.Equal(bd.AddYears(2), pf.LastSensitivityDate);

            var deets = PortfolioEx.Details(pf);

            Assert.Throws<NotImplementedException>(() => pf.TradeId);
            Assert.Throws<NotImplementedException>(() => pf.Counterparty);
            Assert.Throws<NotImplementedException>(() => pf.Counterparty = null);


        }

    }
}
