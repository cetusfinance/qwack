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
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Core.Models;
using Qwack.Core.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Models;

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
                
                var cObj = new HazzardCurve(BuildDate, dayCountBasis, new ConstantHazzardInterpolator(HazzardRate));
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

    }
}
