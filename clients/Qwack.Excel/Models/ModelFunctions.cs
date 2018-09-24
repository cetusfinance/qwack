using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Core.Basic;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Core.Cubes;
using Qwack.Models.MCModels;

namespace Qwack.Excel.Curves
{
    public class ModelFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<ModelFunctions>();


        [ExcelFunction(Description = "Creates a monte-carlo settings object", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(CreateMcSettings))]
        public static object CreateMcSettings(
           [ExcelArgument(Description = "Settings object name")] string ObjectName,
           [ExcelArgument(Description = "Number of paths")] int NumberOfPaths,
           [ExcelArgument(Description = "Number of timesteps")] int NumberOfTimesteps,
           [ExcelArgument(Description = "Random number generator, e.g. Sobol or MersenneTwister")] object RandomGenerator)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var rGen = RandomGenerator.OptionalExcel("MersenneTwister");

                if (!Enum.TryParse(rGen, out RandomGeneratorType randomGenerator))
                {
                    return $"Could not parse random generator name - {rGen}";
                }

                var settings = new McSettings
                {
                    Generator = randomGenerator,
                    NumberOfPaths = NumberOfPaths,
                    NumberOfTimesteps = NumberOfTimesteps
                };
                var settingsCache = ContainerStores.GetObjectCache<McSettings>();
                settingsCache.PutObject(ObjectName, new SessionItem<McSettings> { Name = ObjectName, Value = settings });
                return ObjectName + '¬' + settingsCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns PV of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioPV))]
        public static object McPortfolioPV(
          [ExcelArgument(Description = "Result object name")] string ResultObjectName,
          [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
          [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
          [ExcelArgument(Description = "MC settings name")] string SettingsName,
          [ExcelArgument(Description = "Reporting currency (optional)")] object ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");

                Currency ccy = null;
                if (!(ReportingCcy is ExcelMissing))
                {
                    ccy = new Currency(ReportingCcy as string, DayCountBasis.Act365F, null);
                }

                var mc = new AssetFxLocalVolMC(model.Value.BuildDate, pfolio, model.Value, settings.Value);

                var result = mc.PV(ccy);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }
    }
}
