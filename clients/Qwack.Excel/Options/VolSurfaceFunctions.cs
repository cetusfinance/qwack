using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Options;
using Qwack.Excel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Options.VolSurfaces;
using Qwack.Excel.Utils;

namespace Qwack.Excel.Options
{
    public class VolSurfaceFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<AmericanFunctions>();

        [ExcelFunction(Description = "Creates a constant vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateConstantVolSurface))]
        public static object CreateConstantVolSurface(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
            [ExcelArgument(Description = "Volatility")] double Volatility)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var surface = new ConstantVolSurface(OriginDate, Volatility);
                var cache = ContainerStores.GetObjectCache<ConstantVolSurface>();
                cache.PutObject(ObjectName, new SessionItem<ConstantVolSurface> { Name = ObjectName, Value = surface });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a constant vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateConstantVolSurface))]
        public static object CreateGridVolSurface(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Strikes")] double[] Strikes,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "Volatilities")] double[,] Volatilities,
              [ExcelArgument(Description = "Stike Type - default Absolute")] object StrikeType,
              [ExcelArgument(Description = "Stike Interpolation - default Linear")] object StrikeInterpolation,
              [ExcelArgument(Description = "Time Interpolation - default Linear")] object TimeInterpolation
              )
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var stikeType = StrikeType.OptionalExcel<string>("Absolute");

                var surface = new ConstantVolSurface(OriginDate, Volatility);
                var cache = ContainerStores.GetObjectCache<ConstantVolSurface>();
                cache.PutObject(ObjectName, new SessionItem<ConstantVolSurface> { Name = ObjectName, Value = surface });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }
    }
}
