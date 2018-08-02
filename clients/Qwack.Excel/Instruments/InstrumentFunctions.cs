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

namespace Qwack.Excel.Curves
{
    public class InstrumentFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<InstrumentFunctions>();
        //  public static AsianSwapStrip CreateMonthlyAsianSwap(string period, double strike, string assetId, 
        //Calendar fixingCalendar, Calendar payCalendar, Frequency payOffset, 
        //Currency currency, TradeDirection tradeDirection=TradeDirection.Long, 
        //Frequency spotLag = new Frequency(), double notional=1, 
        //DateGenerationType fixingDateType=DateGenerationType.BusinessDays)

        [ExcelFunction(Description = "Creates an asian swap", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAsianSwap))]
        public static object CreateAsianSwap(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Period code")] string PeriodCode,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Strike")] double Strike,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
             [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
             [ExcelArgument(Description = "Payment offset")] object PaymentOffset,
             [ExcelArgument(Description = "Spot lag")] object SpotLag,
             [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var paymentOffset = PaymentOffset.OptionalExcel("0b");
                var spotLag = PaymentOffset.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = new Core.Basic.Currency(Currency, DayCountBasis.Act365F, pCal);

                var product = AssetProductFactory.CreateMonthlyAsianSwap(PeriodCode, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);

                var cache = ContainerStores.GetObjectCache<AsianSwapStrip>();
                cache.PutObject(ObjectName, new SessionItem<AsianSwapStrip> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

     
    }
}
