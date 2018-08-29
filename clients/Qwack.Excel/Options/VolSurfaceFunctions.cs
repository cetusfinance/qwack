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
using Qwack.Core.Basic;

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
                var surface = new ConstantVolSurface(OriginDate, Volatility)
                {
                    Name = ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IVolSurface>();
                cache.PutObject(ObjectName, new SessionItem<IVolSurface> { Name = ObjectName, Value = surface });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a grid vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateGridVolSurface))]
        public static object CreateGridVolSurface(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Strikes")] double[] Strikes,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "Volatilities")] double[,] Volatilities,
              [ExcelArgument(Description = "Stike Type - default Absolute")] object StrikeType,
              [ExcelArgument(Description = "Stike Interpolation - default Linear")] object StrikeInterpolation,
              [ExcelArgument(Description = "Time Interpolation - default Linear")] object TimeInterpolation,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels
              )
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var stikeType = StrikeType.OptionalExcel<string>("Absolute");
                var expiries = ExcelHelper.ToDateTimeArray(Expiries);
                var surface = new GridVolSurface(OriginDate, Strikes, expiries, Volatilities.SquareToJagged(), labels)
                {
                    Name = ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IVolSurface>();
                cache.PutObject(ObjectName, new SessionItem<IVolSurface> { Name = ObjectName, Value = surface });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Gets a volatility for a delta strike from a vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GetVolForDeltaStrike))]
        public static object GetVolForDeltaStrike(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Delta Strike")] double DeltaStrike,
             [ExcelArgument(Description = "Expiry")] DateTime Expiry,
             [ExcelArgument(Description = "Forward")] double Forward
             )
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(ObjectName, out var volSurface))
                {
                    return volSurface.Value.GetVolForDeltaStrike(DeltaStrike, Expiry, Forward);
                }

                return $"Vol surface {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Gets a volatility for an absolute strike from a vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GetVolForAbsoluteStrike))]
        public static object GetVolForAbsoluteStrike(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Absolute Strike")] double Strike,
             [ExcelArgument(Description = "Expiry")] DateTime Expiry,
             [ExcelArgument(Description = "Forward")] double Forward
             )
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(ObjectName, out var volSurface))
                {
                    return volSurface.Value.GetVolForAbsoluteStrike(Strike, Expiry, Forward);
                }

                return $"Vol surface {ObjectName} not found in cache";
            });
        }
    }
}
