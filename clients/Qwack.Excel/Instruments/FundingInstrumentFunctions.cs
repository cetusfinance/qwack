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

namespace Qwack.Excel.Curves
{
    public class FundingInstrumentFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<FundingInstrumentFunctions>();

        
        [ExcelFunction(Description = "Creates a FRA object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFRA))]
        public static object CreateFRA(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Value date")] DateTime ValDate,
             [ExcelArgument(Description = "FRA code e.g. 3X6")] string PeriodCode,
             [ExcelArgument(Description = "Rate Index")] string RateIndex,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Par Rate")] double ParRate,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Forecast Curve")] string ForecastCurve,
             [ExcelArgument(Description = "Discount Curve")] string DiscountCurve,
             [ExcelArgument(Description = "DiscountingType")] object DiscountingType,
             [ExcelArgument(Description = "Pay / Receive")] object PayRec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var discType = DiscountingType.OptionalExcel("Isda");
                var payRec = PayRec.OptionalExcel("Pay");
           
                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndex);
                    return $"Rate index {RateIndex} not found in cache";
                }

                if (!Enum.TryParse(payRec, out SwapPayReceiveType pType))
                {
                    return $"Could not parse pay/rec - {payRec}";
                }

                if (!Enum.TryParse(discType, out FraDiscountingType fType))
                {
                    return $"Could not parse FRA discounting type - {discType}";
                }

                var product = new ForwardRateAgreement(ValDate, PeriodCode, ParRate, rIndex.Value, pType, fType, ForecastCurve, DiscountCurve);

                var cache = ContainerStores.GetObjectCache<ForwardRateAgreement>();
                cache.PutObject(ObjectName, new SessionItem<ForwardRateAgreement> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

     
    }
}
