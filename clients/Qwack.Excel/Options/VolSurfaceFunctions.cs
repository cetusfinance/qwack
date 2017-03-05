using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Options;
using Qwack.Excel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Qwack.Excel.Options
{
    public class VolSurfaceFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<AmericanFunctions>();

        [ExcelFunction(Description = "Creates a constant vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateConstantVolSurface))]
        public static object CreateConstantVolSurface(
            [ExcelArgument(Description = "Object name")] double Name,
            [ExcelArgument(Description = "Origin date")] double OriginDate,
            [ExcelArgument(Description = "Volatility")] double Volatility)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                //ContainerStores.SessionContainer

                return null;
            });
        }

     
    }
}
