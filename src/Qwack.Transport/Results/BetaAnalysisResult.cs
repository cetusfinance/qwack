using System;
using System.Collections.Generic;

namespace Qwack.Transport.Results
{
    public class BetaAnalysisResult
    {
        public LinearRegressionResult LrResult { get; set; }
        public Dictionary<string, BetaAnalysisResult> TradeBreakdown { get; set; }
        public double[] BenchmarkReturns { get; set; }
        public double[] PortfolioReturns { get; set; }
        public Dictionary<DateTime, double> BenchmarkPrices { get; set; }
    }
}
