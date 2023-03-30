using System;
using ExcelDna.Integration;
using Qwack.Excel.Services;
using Qwack.Dates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Transport.BasicTypes;

namespace Qwack.Excel.Options
{
    public class LMEFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<LMEFunctions>();

        [ExcelFunction(Description = "Returns option PV using the LME-modified Black'76 formula", Category = "QOpt")]
        public static object QOpt_LMEBlackPV(
            [ExcelArgument(Description = "Today/Value date (origin)")] DateTime ValueDate,
            [ExcelArgument(Description = "Expiry date (1st Wednesday)")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Delivery date (3rd Wednesday)")] DateTime DeliveryDate,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger,() =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                    return $"Could not parse call or put flag - {CP}";

                var tExpiry = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, ExpiryDate);
                var tDelivery = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, DeliveryDate);

                return Qwack.Options.LMEFunctions.LMEBlackPV(F, K, R, tExpiry, tDelivery, V, optType);
            });
        }

        [ExcelFunction(Description = "Returns option delta on forward basis using the LME-modified Black'76 formula", Category = "QOpt")]
        public static object QOpt_LMEBlackDelta(
            [ExcelArgument(Description = "Today/Value date (origin)")] DateTime ValueDate,
            [ExcelArgument(Description = "Expiry date (1st Wednesday)")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                    return $"Could not parse call or put flag - {CP}";

                var tExpiry = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, ExpiryDate);
  
                return Qwack.Options.LMEFunctions.LMEBlackDelta(F, K, tExpiry, V, optType);
            });
        }

        [ExcelFunction(Description = "Returns option gamma on forward basis using the LME-modified Black'76 formula", Category = "QOpt")]
        public static object QOpt_LMEBlackGamma(
            [ExcelArgument(Description = "Today/Value date (origin)")] DateTime ValueDate,
            [ExcelArgument(Description = "Expiry date (1st Wednesday)")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Volatility")] double V)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var tExpiry = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, ExpiryDate);

                return Qwack.Options.LMEFunctions.LMEBlackGamma(F, K, tExpiry, V);
            });
        }

        [ExcelFunction(Description = "Returns option vega using the LME-modified Black'76 formula", Category = "QOpt")]
        public static object QOpt_LMEBlackVega(
            [ExcelArgument(Description = "Today/Value date (origin)")] DateTime ValueDate,
            [ExcelArgument(Description = "Expiry date (1st Wednesday)")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Delivery date (3rd Wednesday)")] DateTime DeliveryDate,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var tExpiry = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, ExpiryDate);
                var tDelivery = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, DeliveryDate);

                return Qwack.Options.LMEFunctions.LMEBlackVega(F, K, R, tExpiry, tDelivery, V);
            });
        }

        [ExcelFunction(Description = "Returns an implied volatility using the LME-modified Black'76 formula", Category = "QOpt")]
        public static object QOpt_LMEBlackImpliedVol(
            [ExcelArgument(Description = "Today/Value date (origin)")] DateTime ValueDate,
            [ExcelArgument(Description = "Expiry date (1st Wednesday)")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Delivery date (3rd Wednesday)")] DateTime DeliveryDate,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Option Premium")] double PV,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse<OptionType>(CP, out var optType))
                    return $"Could not parse call or put flag - {CP}";

                var tExpiry = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, ExpiryDate);
                var tDelivery = DayCountBasis.Act_365F.CalculateYearFraction(ValueDate, DeliveryDate);

                return Qwack.Options.LMEFunctions.LMEBlackImpliedVol(F, K, R, tExpiry, tDelivery, PV, optType);
            });
        }
    }
}
