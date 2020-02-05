using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Math.Interpolation;
using Qwack.Dates;

namespace Qwack.Excel.Curves
{
    public class CreditCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<CreditCurveFunctions>();

        [ExcelFunction(Description = "Creates a credit curve with constant hazzard rate", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateConstantHazzardCurve))]
        public static object CreateConstantHazzardCurve(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Hazzard rate")] double HazzardRate,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Basis, default Act365F")] object Basis)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var basis = Basis.OptionalExcel("Act365F");
                if (!Enum.TryParse(basis, out DayCountBasis dayCountBasis))
                    throw new Exception($"Could not parse basis type - {basis}");

                var cObj = new HazzardCurve(BuildDate, dayCountBasis, new ConstantHazzardInterpolator(HazzardRate))
                {
                    ConstantPD = HazzardRate
                };
                var cache = ContainerStores.GetObjectCache<HazzardCurve>();
                cache.PutObject(ObjectName, new SessionItem<HazzardCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Gets survival probability from a hazzard curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetSurvivalProbability))]
        public static object GetSurvivalProbability(
             [ExcelArgument(Description = "Curve object name")] string ObjectName,
             [ExcelArgument(Description = "Start date")] DateTime StartDate,
             [ExcelArgument(Description = "End date")] DateTime EndDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return ContainerStores
                .GetObjectCache<HazzardCurve>()
                .GetObjectOrThrow(ObjectName, $"Could not find curve {ObjectName}")
                .Value.GetSurvivalProbability(StartDate, EndDate);
            });
        }


        [ExcelFunction(Description = "Gets risky discount factor from a pair of hazzard and discount curves", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetRiskyDiscountFactor))]
        public static object GetRiskyDiscountFactor(
             [ExcelArgument(Description = "Credit curve object name")] string ObjectName,
             [ExcelArgument(Description = "Discount curve object name")] string DiscoObjectName,
             [ExcelArgument(Description = "Start date")] DateTime StartDate,
             [ExcelArgument(Description = "End date")] DateTime EndDate,
             [ExcelArgument(Description = "LGD, e.g. 0.45")] double LGD)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var disco = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscoObjectName, $"Could not find discount curve {DiscoObjectName}");
                return ContainerStores
                .GetObjectCache<HazzardCurve>()
                .GetObjectOrThrow(ObjectName, $"Could not find curve {ObjectName}")
                .Value.RiskyDiscountFactor(StartDate, EndDate, disco.Value, LGD);
            });
        }
    }
}
