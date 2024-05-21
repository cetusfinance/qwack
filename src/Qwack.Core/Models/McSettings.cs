using System.Collections.Generic;
using ProtoBuf;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Core.Models
{
    public class McSettings
    {
        public int NumberOfPaths { get; set; }
        public int NumberOfTimesteps { get; set; }
        public RandomGeneratorType Generator { get; set; }
        public Currency ReportingCurrency { get; set; }
        public bool ExpensiveFuturesSimulation { get; set; }
        public Dictionary<string, string> FuturesMappingTable { get; set; } = new Dictionary<string, string>();
        public McModelType McModelType { get; set; }
        public bool LocalCorrelation { get; set; }
        public bool Parallelize { get; set; }
        public bool DebugMode { get; set; }
        public bool AveragePathCorrection { get; set; }
        public bool CompactMemoryMode { get; set; }
        public bool AvoidRegressionForBackPricing { get; set; }
        public CreditSettings CreditSettings { get; set; } = new CreditSettings();

        public McSettings() { }
        public McSettings(TO_McSettings to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            AveragePathCorrection = to.AveragePathCorrection;
            AvoidRegressionForBackPricing = to.AvoidRegressionForBackPricing;
            CompactMemoryMode = to.CompactMemoryMode;
            DebugMode = to.DebugMode;
            ExpensiveFuturesSimulation = to.ExpensiveFuturesSimulation;
            FuturesMappingTable = to.FuturesMappingTable;
            Generator = to.Generator;
            LocalCorrelation = to.LocalCorrelation;
            McModelType = to.McModelType;
            NumberOfPaths = to.NumberOfPaths;
            NumberOfTimesteps = to.NumberOfTimesteps;
            Parallelize = to.Parallelize;
            ReportingCurrency = currencyProvider.GetCurrencySafe(to.ReportingCurrency);
            CreditSettings = to.CreditSettings==null ? new CreditSettings() : new CreditSettings(to.CreditSettings, currencyProvider, calendarProvider);
        }

        public McSettings Clone() => new()
        {
            AveragePathCorrection = AveragePathCorrection,
            AvoidRegressionForBackPricing = AvoidRegressionForBackPricing,
            CompactMemoryMode = CompactMemoryMode,
            DebugMode = DebugMode,
            ExpensiveFuturesSimulation = ExpensiveFuturesSimulation,
            FuturesMappingTable = FuturesMappingTable,
            Generator = Generator,
            LocalCorrelation = LocalCorrelation,
            McModelType = McModelType,
            NumberOfPaths = NumberOfPaths,
            NumberOfTimesteps = NumberOfTimesteps,
            Parallelize = Parallelize,
            ReportingCurrency = ReportingCurrency,
            CreditSettings = CreditSettings.Clone(),
        };

        public TO_McSettings GetTransportObject() => new()
        {
            AveragePathCorrection = AveragePathCorrection,
            AvoidRegressionForBackPricing = AvoidRegressionForBackPricing,
            CompactMemoryMode = CompactMemoryMode,
            DebugMode = DebugMode,
            ExpensiveFuturesSimulation = ExpensiveFuturesSimulation,
            FuturesMappingTable = FuturesMappingTable,
            Generator = Generator,
            LocalCorrelation = LocalCorrelation,
            McModelType = McModelType,
            NumberOfPaths = NumberOfPaths,
            NumberOfTimesteps = NumberOfTimesteps,
            Parallelize = Parallelize,
            ReportingCurrency = ReportingCurrency,
            //CreditSettings = CreditSettings.(),
        };
    }
}
