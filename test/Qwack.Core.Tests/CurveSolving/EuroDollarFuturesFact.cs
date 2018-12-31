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
using Qwack.Math.Utils;
using Qwack.Models;
using Qwack.Providers.Json;
using Xunit;

namespace Qwack.Core.Tests.CurveSolving
{
    public class EuroDollarFuturesFact
    {
        [Fact]
        public void FuturesStripNoConvexity()
        {
            var startDate = new DateTime(2017, 01, 17);
            var nContracts = 24;
            var currentDate = startDate.GetNextImmDate();
            var expiries = new DateTime[nContracts];
            var pillars = new DateTime[nContracts];
            var instruments = new IFundingInstrument[nContracts];

            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];
            var lon = TestProviderHelper.CalendarProvider.Collection["LON"];
            var ccyUsd = TestProviderHelper.CurrencyProvider["USD"];

            var usd3m = new FloatRateIndex()
            {
                Currency = ccyUsd,
                DayCountBasis = DayCountBasis.Act_360,
                DayCountBasisFixed = DayCountBasis.Act_360,
                ResetTenor = 3.Months(),
                FixingOffset = 2.Bd(),
                HolidayCalendars = nyc,
                RollConvention = RollType.MF
            };

            for (var i =0;i<nContracts;i++)
            {
                var wed3rd = currentDate.ThirdWednesday();
                expiries[i] = wed3rd.SubtractPeriod(RollType.P, lon, 2.Bd());
                instruments[i] = new STIRFuture()
                {
                    Currency = ccyUsd,
                    ContractSize = 1e6,
                    ConvexityAdjustment = 0,
                    DCF = 0.25,
                    Expiry = expiries[i],
                    ForecastCurve = "USD.LIBOR.3M",
                    Index = usd3m,
                    Position = 1.0,
                    Price = 99.50,
                    SolveCurve = "USD.LIBOR.3M"
                };
                pillars[i] = wed3rd.AddPeriod(usd3m.RollConvention, usd3m.HolidayCalendars, usd3m.ResetTenor);
                currentDate = currentDate.AddMonths(3);
            }

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider);
            fic.AddRange(instruments);

            var curve = new IrCurve(pillars, new double[nContracts], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap, ccyUsd);
            var model = new FundingModel(startDate, new[] { curve }, TestProviderHelper.CurrencyProvider);

            var s = new Calibrators.NewtonRaphsonMultiCurveSolver();
            s.Solve(model, fic);

            for (var i = 0; i < nContracts; i++)
            {
                var resultPV = instruments[i].Pv(model, false);
                Assert.Equal(0, resultPV, 6);
            }
        }

        [Fact]
        public void FuturesStripWithConvexity()
        {
            var volatility = 0.03;

            var startDate = new DateTime(2017, 01, 17);
            var nContracts = 24;
            var currentDate = startDate.GetNextImmDate();
            var expiries = new DateTime[nContracts];
            var pillars = new DateTime[nContracts];
            var instruments = new IFundingInstrument[nContracts];

            var nyc = TestProviderHelper.CalendarProvider.Collection["NYC"];
            var lon = TestProviderHelper.CalendarProvider.Collection["LON"];
            var ccyUsd = TestProviderHelper.CurrencyProvider["USD"];

            var usd3m = new FloatRateIndex()
            {
                Currency = ccyUsd,
                DayCountBasis = DayCountBasis.Act_360,
                DayCountBasisFixed = DayCountBasis.Act_360,
                ResetTenor = 3.Months(),
                FixingOffset = 2.Bd(),
                HolidayCalendars = nyc,
                RollConvention = RollType.MF
            };

            for (var i = 0; i < nContracts; i++)
            {
                var wed3rd = currentDate.ThirdWednesday();
                expiries[i] = wed3rd.SubtractPeriod(RollType.P, lon, 2.Bd());
                pillars[i] = wed3rd.AddPeriod(usd3m.RollConvention, usd3m.HolidayCalendars, usd3m.ResetTenor);
                instruments[i] = new STIRFuture()
                {
                    Currency = ccyUsd,
                    ContractSize = 1e6,
                    ConvexityAdjustment = FuturesConvexityUtils.CalculateConvexityAdjustment(startDate, expiries[i], pillars[i], volatility),
                    DCF = 0.25,
                    Expiry = expiries[i],
                    ForecastCurve = "USD.LIBOR.3M",
                    Index = usd3m,
                    Position = 1.0,
                    Price = 99.50,
                    SolveCurve = "USD.LIBOR.3M"
                };

                currentDate = currentDate.AddMonths(3);
            }

            var fic = new FundingInstrumentCollection(TestProviderHelper.CurrencyProvider);
            fic.AddRange(instruments);

            var curve = new IrCurve(pillars, new double[nContracts], startDate, "USD.LIBOR.3M", Interpolator1DType.LinearFlatExtrap, ccyUsd);
            var model = new FundingModel(startDate, new[] { curve }, TestProviderHelper.CurrencyProvider);

            var s = new Calibrators.NewtonRaphsonMultiCurveSolver();
            s.Solve(model, fic);

            for (var i = 0; i < nContracts; i++)
            {
                var resultPV = instruments[i].Pv(model, false);
                Assert.Equal(0, resultPV, 6);
            }
        }
    }
}
