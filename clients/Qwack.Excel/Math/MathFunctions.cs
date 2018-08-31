using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Math.Interpolation;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Qwack.Excel.Interpolation
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
                return Math.Statistics.FisherTransform(Correlation, ConfidenceLevel, SampleSize, IsBid);
            });
        }
    }
}
