using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

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

        public McSettings Clone() => new McSettings
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
            CreditSettings = CreditSettings.Clone()
        };
    }
}
