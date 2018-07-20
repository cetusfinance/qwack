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
    public class InterpolatorFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<InterpolatorFunctions>();

        [ExcelFunction(Description = "Creates a 1-dimensional interpolator", Category = CategoryNames.Interpolation, Name = CategoryNames.Interpolation + "_" + nameof(Create1dInterpolator))]
        public static object Create1dInterpolator(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Array of X values")] double[] X,
             [ExcelArgument(Description = "Array of Y values")] double[] Y,
             [ExcelArgument(Description = "Type of interpolator, e.g. Linear")] string InterpolatorType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var interpType = InterpolatorType.OptionalExcel<string>("Linear");
                if (!Enum.TryParse(interpType, out Interpolator1DType iType))
                {
                    return $"Could not parse 1d interpolator type - {interpType}";
                }

                var iObj = InterpolatorFactory.GetInterpolator(X, Y, iType);
                var cache = ContainerStores.GetObjectCache<IInterpolator1D>();
                cache.PutObject(ObjectName, new SessionItem<IInterpolator1D> { Name = ObjectName, Value = iObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a 2-dimensional interpolator", Category = CategoryNames.Interpolation, Name = CategoryNames.Interpolation + "_" + nameof(Create2dInterpolator))]
        public static object Create2dInterpolator(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Array of X values")] double[] X,
             [ExcelArgument(Description = "Array of Y values")] double[] Y,
             [ExcelArgument(Description = "2d array of Z values")] double[,] Z,
             [ExcelArgument(Description = "Type of interpolator, e.g. Bilinear")] string InterpolatorType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var interpType = InterpolatorType.OptionalExcel<string>("Bilinear");
                if (!Enum.TryParse(interpType, out Interpolator2DType iType))
                {
                    return $"Could not parse 2d interpolator type - {interpType}";
                }

                var iObj = InterpolatorFactory.GetInterpolator(X, Y, Z, iType);
                var cache = ContainerStores.GetObjectCache<IInterpolator2D>();
                cache.PutObject(ObjectName, new SessionItem<IInterpolator2D> { Name = ObjectName, Value = iObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Queries a 1-dimensional interpolator", Category = CategoryNames.Interpolation, Name = CategoryNames.Interpolation + "_" + nameof(Interpolate1d))]
        public static object Interpolate1d(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "X value to interpolate")] double X)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IInterpolator1D>().TryGetObject(ObjectName, out var interpolator))
                {
                    return interpolator.Value.Interpolate(X);
                }

                return $"1d interpolator {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Queries a 2-dimensional interpolator", Category = CategoryNames.Interpolation, Name = CategoryNames.Interpolation + "_" + nameof(Interpolate2d))]
        public static object Interpolate2d(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "X value to interpolate")] double X,
            [ExcelArgument(Description = "Y value to interpolate")] double Y)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IInterpolator2D>().TryGetObject(ObjectName, out var interpolator))
                {
                    return interpolator.Value.Interpolate(X,Y);
                }

                return $"2d interpolator {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Queries a 1-dimensional interpolator and returns average value", Category = CategoryNames.Interpolation, Name = CategoryNames.Interpolation + "_" + nameof(Interpolate1dAverage))]
        public static object Interpolate1dAverage(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "X values to interpolate")] double[] Xs)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IInterpolator1D>().TryGetObject(ObjectName, out var interpolator))
                {
                    return interpolator.Value.Average(Xs);
                }

                return $"1d interpolator {ObjectName} not found in cache";
            });
        }
    }
}
