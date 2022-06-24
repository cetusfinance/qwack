using System;
using System.Collections.Generic;
using static Qwack.Math.LinearRegression;

namespace Qwack.Models.Risk
{
    public class BetaAnalysisResult
    {
        public LinearRegressionResult LrResult { get; set; }
        public double[] BenchmarkReturns { get; set; }
        public double[] PortfolioReturns { get; set; }
        public Dictionary<DateTime, double> BenchmarkPrices { get; set; }
    }
}
