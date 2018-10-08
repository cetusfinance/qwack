using System;
using ExcelDna.Integration;
using Qwack.Excel.Services;
using Qwack.Core.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Qwack.Excel.Options
{
    public class BlackFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<BlackFunctions>();

        [ExcelFunction(Description = "Returns option PV using the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(BlackPV))]
        public static object BlackPV(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.BlackFunctions.BlackPV(F, K, R, T, V, optType);
            });
        }

        [ExcelFunction(Description = "Returns option delta using the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(BlackDelta))]
        public static object BlackDelta(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.BlackFunctions.BlackDelta(F, K, R, T, V, optType);
            });
        }

        [ExcelFunction(Description = "Returns option gamma using the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(BlackGamma))]
        public static object BlackGamma(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                return Qwack.Options.BlackFunctions.BlackGamma(F, K, R, T, V);
            });
        }

        [ExcelFunction(Description = "Returns option vega using the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(BlackVega))]
        public static object BlackVega(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                return Qwack.Options.BlackFunctions.BlackVega(F, K, R, T, V);
            });
        }

        [ExcelFunction(Description = "Returns an implied volatility using the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(BlackImpliedVol))]
        public static object BlackImpliedVol(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Option Premium")] double PV,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.BlackFunctions.BlackImpliedVol(F, K, R, T, PV, optType);
            });
        }

        [ExcelFunction(Description = "Returns an absolute strike for a given delta and vol according to the Black'76 formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(AbsoluteStrikeFromDelta))]
        public static object AbsoluteStrikeFromDelta(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Delta strike")] double DeltaK,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Volatility")] double Sigma,
            [ExcelArgument(Description = "Discounting rate")] double R)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return Qwack.Options.BlackFunctions.AbsoluteStrikefromDeltaKAnalytic(F, DeltaK, R, T, Sigma);
            });
        }
    }
}
