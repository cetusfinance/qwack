using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Math;
using Qwack.Math.Distributions;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Qwack.Excel.Math
{
    public class MathFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<MathFunctions>();

        [ExcelFunction(Description = "Returns fisher transform on correlation for a given sample size and confidence level", Category = CategoryNames.Math, Name = CategoryNames.Math + "_" + nameof(FisherTransform))]
        public static object FisherTransform(
             [ExcelArgument(Description = "Input correlation")] double Correlation,
             [ExcelArgument(Description = "Confidence level, e.g. 0.75")] double ConfidenceLevel,
             [ExcelArgument(Description = "Sample size, e.g. 90")] double SampleSize,
             [ExcelArgument(Description = "True for bid, false for offer")] bool IsBid)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return Statistics.FisherTransform(Correlation, ConfidenceLevel, SampleSize, IsBid);
            });
        }

        [ExcelFunction(Description = "Performs linear regression on a set of samples", Category = CategoryNames.Math, Name = CategoryNames.Math + "_" + nameof(LinearRegression))]
        public static object LinearRegression(
             [ExcelArgument(Description = "X values")] double[] Xs,
             [ExcelArgument(Description = "Y values")] double[] Ys)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var result = Xs.LinearRegressionNoVector(Ys, true);
                var o = new object[,]
                {
                    {result.Alpha,"Alpha"},
                    {result.Beta,"Beta"},
                    {result.R2,"R2"},
                    {result.SSE,"SSE"}
                };
                return o;
            });
        }

        [ExcelFunction(Description = "Returns value from standard (zero-mean, unit std dev) bivariate normal distribution", Category = CategoryNames.Math, Name = CategoryNames.Math + "_" + nameof(BivariateNormalStdPDF))]
        public static object BivariateNormalStdPDF(
             [ExcelArgument(Description = "X value")] double X,
             [ExcelArgument(Description = "Y value")] double Y,
             [ExcelArgument(Description = "Correlation")] double Correlation)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return BivariateNormal.PDF(X, Y, Correlation);
            });
        }

        [ExcelFunction(Description = "Returns value from bivariate normal distribution", Category = CategoryNames.Math, Name = CategoryNames.Math + "_" + nameof(BivariateNormalPDF))]
        public static object BivariateNormalPDF(
             [ExcelArgument(Description = "X value")] double X,
             [ExcelArgument(Description = "X mean")] double Xbar,
             [ExcelArgument(Description = "X std deviation")] double XStdDev,
             [ExcelArgument(Description = "Y values")] double Y,
             [ExcelArgument(Description = "Y mean")] double Ybar,
             [ExcelArgument(Description = "Y std deviation")] double YStdDev,
             [ExcelArgument(Description = "Correlation")] double Correlation)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return BivariateNormal.PDF(X, Xbar, XStdDev, Y, Ybar, YStdDev, Correlation);
            });
        }

        [ExcelFunction(Description = "Returns CDF value from standard (zero-mean, unit std dev) bivariate normal distribution", Category = CategoryNames.Math, Name = CategoryNames.Math + "_" + nameof(BivariateNormalStdCDF))]
        public static object BivariateNormalStdCDF(
            [ExcelArgument(Description = "X value")] double X,
            [ExcelArgument(Description = "Y value")] double Y,
            [ExcelArgument(Description = "Correlation")] double Correlation)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return BivariateNormal.CDF(X, Y, Correlation);
            });
        }
    }
}
