using System;
using ExcelDna.Integration;
using Qwack.Options;
using Qwack.Excel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Transport.BasicTypes;

namespace Qwack.Excel.Options
{
    public class AmericanFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<AmericanFunctions>();

        [ExcelFunction(Description = "Returns an american futures option PV using a grid", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(AmericanFutureOptionPV))]
        public static object AmericanFutureOptionPV(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP,
            [ExcelArgument(Description = "Pricing method (Defult Trinomial)")] string Method)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                if (!Enum.TryParse(Method, out AmericanPricingType method))
                {
                    return $"Could not parse pricing type - {Method}";
                }
                if (method == AmericanPricingType.Binomial)
                {
                    return BinomialTree.AmericanFutureOptionPV(F, K, R, T, V, optType);
                }
                else
                {
                    return TrinomialTree.AmericanFutureOptionPV(F, K, R, T, V, optType);
                }
            });
        }

        [ExcelFunction(Description = "Returns an implied volatility for an american futures option PV using a grid", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(AmericanFutureOptionImpliedVol))]
        public static object AmericanFutureOptionImpliedVol(
          [ExcelArgument(Description = "Time-to-expiry")] double T,
          [ExcelArgument(Description = "Strike")] double K,
          [ExcelArgument(Description = "Forward")] double F,
          [ExcelArgument(Description = "Discounting rate")] double R,
          [ExcelArgument(Description = "Option Premium")] double PV,
          [ExcelArgument(Description = "Call or Put")] string CP,
          [ExcelArgument(Description = "Pricing method (Defult Trinomial)")] string Method)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                if (!Enum.TryParse(Method, out AmericanPricingType method))
                {
                    return $"Could not parse pricing type - {Method}";
                }
                if (method == AmericanPricingType.Binomial)
                {
                    return BinomialTree.AmericanFuturesOptionImpliedVol(F, K, R, T, PV, optType);
                }
                else
                {
                    return TrinomialTree.AmericanFuturesOptionImpliedVol(F, K, R, T, PV, optType);
                }
            });
        }
    }
}
