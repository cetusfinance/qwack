using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Risk
{
    public static class QuickPFECalculator
    {
        public static ICube Calculate(IAssetFxModel model, Portfolio portfolio, double confidenceInterval, Currency reportingCcy, ICurrencyProvider currencyProvider,
            DateTime[] exposureDates = null, DatePeriodType sampleFreq = DatePeriodType.Month)
        {
            if (exposureDates == null)
                exposureDates = portfolio.ComputeSimDates(model.BuildDate, sampleFreq);

            var m = model.TrimModel(portfolio);

            var o = new double[exposureDates.Length];
            for(var i=0;i<exposureDates.Length;i++)
            {
                m = m.RollModelPfe(exposureDates[i], confidenceInterval, currencyProvider);
                o[i] = portfolio.PV(m, reportingCcy, true).SumOfAllRows;
            }

            return PackToCube(exposureDates, o, "PFE");
        }

        public static ICube PackToCube(DateTime[] dates, double[] values, string metric)
        {
            if (dates.Length != values.Length)
                throw new DataMisalignedException();

            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "ExposureDate", typeof(DateTime) },
                { "Metric", typeof(string) }
            };
            cube.Initialize(dataTypes);

            for (var i = 0; i < values.Length; i++)
            {
                cube.AddRow(new object[] { dates[i], metric }, values[i]);
            }
            return cube;
        }
    }
}
