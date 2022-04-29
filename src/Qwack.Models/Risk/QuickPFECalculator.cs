using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;
using static System.Math;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk
{
    public static class QuickPFECalculator
    {
        private static readonly Type[] AllowedTypes = { typeof(AsianSwap), typeof(AsianSwapStrip), typeof(AsianOption), typeof(Forward), typeof(EuropeanOption) };
        public static ICube Calculate(IAssetFxModel model, Portfolio portfolio, double confidenceInterval, Currency reportingCcy, ICurrencyProvider currencyProvider,
            ICalendarProvider calendarProvider, DateTime[] exposureDates = null, DatePeriodType sampleFreq = DatePeriodType.Month, bool correlationCorrection = true)
        {
            var types = portfolio.Instruments.Select(x => x.GetType()).Distinct();

            if (!types.All(t => AllowedTypes.Contains(t)))
                throw new Exception("Unsupported instrument types detected");

            var pf = portfolio.UnStripStrips();
            var assetIns = pf.Instruments.Select(x => ((IAssetInstrument)x));
            //check everything facting in same direction
            var pos = assetIns.Select(x => x.IsLongDeltaPosition()).Distinct();
            if (pos.Count() > 1)
                throw new Exception("All trades must have same delta direction");

            //check everything same asset/currency
            var keys = assetIns.Select(x => $"{x.Currency}~{string.Join("-", x.AssetIds)}").Distinct();
            if (keys.Count() > 1)
                throw new Exception("All trades must have same underlying and currency");

            //flip tail of distribution if we are short
            var ci = pos.Single() ? confidenceInterval : 1.0 - confidenceInterval;

            if (exposureDates == null)
                exposureDates = pf.ComputeSimDates(model.BuildDate, sampleFreq);

            var m = model.TrimModel(portfolio);

            var day0Pv = 0.0;
            if (exposureDates.Contains(model.BuildDate))
            {
                day0Pv = Max(0, pf.PV(m, reportingCcy, true).SumOfAllRows);
            }
            //adjust fx surface for correlation if needed
            var pairs = assetIns.Select(x => x.FxPair(model)).Where(x => !string.IsNullOrEmpty(x));
            if (correlationCorrection && pairs.Any())
            {
                var pd = pairs.Distinct();
                if (pd.Count() != 1)
                    throw new Exception("Expecting a single Fx pair, if any");
                var pair = pd.Single();
                var assetId = assetIns.SelectMany(x => x.AssetIds).Distinct().Single();
                var (fxSurface, assetSurface) = model.ImplySurfaceToCorrelation(assetId, pair, assetIns.First().Currency, currencyProvider);
                m.FundingModel.VolSurfaces[pair] = fxSurface;
                m.AddVolSurface(assetId, assetSurface);
            }

            var fxPairsToRoll = assetIns.SelectMany(x => x.AssetIds).Where(x => x.Length == 7 && x.Substring(3, 1) == "/")
                .Concat(assetIns.Select(x => x.FxPair(model)))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct().ToList();

            var o = new double[exposureDates.Length];
            for (var i = 0; i < exposureDates.Length; i++)
            {
                var mm = m.RollModelPfe(exposureDates[i], ci, currencyProvider, calendarProvider, fxPairsToRoll);
                o[i] = exposureDates[i] == model.BuildDate ? day0Pv : Max(0, pf.PV(mm, reportingCcy, true).SumOfAllRows);
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
                { ExposureDate, typeof(DateTime) },
                { Metric, typeof(string) }
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
