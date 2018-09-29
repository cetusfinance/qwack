using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using Qwack.Models;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class SelfDiscountingFact
    {
        public static readonly string JsonCalendarPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Calendars.json");
        public static readonly string JsonCurrencyPath = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Currencies.json");
        public static readonly ICalendarProvider CalendarProvider = CalendarsFromJson.Load(JsonCalendarPath);
        public static readonly ICurrencyProvider CurrencyProvider = ProvideCurrencies(CalendarProvider);

        private static ICurrencyProvider ProvideCurrencies(ICalendarProvider calendarProvider)
        {
            var currencyProvider = new CurrenciesFromJson(calendarProvider, JsonCurrencyPath);
            return currencyProvider;
        }

        [Fact]
        public void BasicSelfDiscounting()
        {
            var startDate = new DateTime(2016, 05, 20);
            var swapTenor2 = new Frequency("2y");
            var jhb = CalendarProvider.Collection["JHB"];

            var pillarDate = startDate.AddPeriod(RollType.MF, jhb, 1.Years());
            var pillarDate2 = startDate.AddPeriod(RollType.MF, jhb, swapTenor2);
            var pillarDateDepo = startDate.AddPeriod(RollType.MF, jhb, 3.Months());

            var ccyZar = CurrencyProvider["JHB"];

            var zar3m = new FloatRateIndex()
            {
                Currency = ccyZar,
                DayCountBasis = DayCountBasis.Act_365F,
                DayCountBasisFixed = DayCountBasis.Act_365F,
                ResetTenor = 3.Months(),
                FixingOffset = 0.Bd(),
                HolidayCalendars = jhb,
                RollConvention = RollType.MF
            };

            var swap = new IrSwap(startDate, 1.Years(), zar3m, 0.06, SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.JIBAR.3M");
            var swap2 = new IrSwap(startDate, swapTenor2, zar3m, 0.06, SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.JIBAR.3M");
            var depo = new IrSwap(startDate, 3.Months(), zar3m, 0.06, SwapPayReceiveType.Payer, "ZAR.JIBAR.3M", "ZAR.JIBAR.3M");

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider)
            {
                swap,
                swap2,
                depo
            };
            var curve = new IrCurve(new [] { pillarDateDepo, pillarDate, pillarDate2 }, new double[3], startDate, "ZAR.JIBAR.3M", Interpolator1DType.LinearFlatExtrap, ccyZar);
            var model = new FundingModel(startDate, new[] { curve }, TestProviderHelper.CurrencyProvider);

            var s = new Calibrators.NewtonRaphsonMultiCurveSolver();
            s.Solve(model, fic);

            var resultSwap1 = swap.Pv(model, false);
            var resultSwap2 = swap2.Pv(model, false);
            var resultDepo = depo.Pv(model, false);

            Assert.Equal(0, resultSwap1, 6);
            Assert.Equal(0, resultSwap2, 6);
            Assert.Equal(0, resultDepo, 6);
        }
    }
}
