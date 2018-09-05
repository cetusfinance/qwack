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
using Qwack.Math.Interpolation;
using Qwack.Core.Curves;

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
              [ExcelArgument(Description = "Time basis - default ACT365F")] object TimeBasis,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels
              )
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var stikeType = StrikeType.OptionalExcel<string>("Absolute");
                var strikeInterpType = StrikeInterpolation.OptionalExcel<string>("Linear");
                var timeInterpType = TimeInterpolation.OptionalExcel<string>("LinearInVariance");
                var timeBasis = TimeBasis.OptionalExcel<string>("ACT365F");

                var expiries = ExcelHelper.ToDateTimeArray(Expiries);

                if (!Enum.TryParse(stikeType, out StrikeType sType))
                    return $"Could not parse strike type - {stikeType}";

                if (!Enum.TryParse(strikeInterpType, out Interpolator1DType siType))
                    return $"Could not parse strike interpolator type - {strikeInterpType}";

                if (!Enum.TryParse(timeInterpType, out Interpolator1DType tiType))
                    return $"Could not parse time interpolator type - {timeInterpType}";

                if (!Enum.TryParse(timeBasis, out Qwack.Dates.DayCountBasis basis))
                    return $"Could not parse time basis type - {timeBasis}";

                var surface = new GridVolSurface(OriginDate, Strikes, expiries, Volatilities.SquareToJagged(), sType, siType, tiType, basis, labels)
                {
                    Name = ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IVolSurface>();
                cache.PutObject(ObjectName, new SessionItem<IVolSurface> { Name = ObjectName, Value = surface });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }


        [ExcelFunction(Description = "Creates a grid vol surface object from RR/BF qoutes", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateRiskyFlyVolSurface))]
        public static object CreateRiskyFlyVolSurface(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Wing deltas")] double[] WingDeltas,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "ATM Volatilities")] double[] ATMVols,
              [ExcelArgument(Description = "Risk Reversal quotes")] double[,] Riskies,
              [ExcelArgument(Description = "Butterfly quotes")] double[,] Flies,
              [ExcelArgument(Description = "Forwards or price curve object")] object FwdsOrCurve,
              [ExcelArgument(Description = "ATM vol type - default zero-delta straddle")] object ATMType,
              [ExcelArgument(Description = "Wing quote type - Simple or Market")] object WingType,
              [ExcelArgument(Description = "Stike Interpolation - default Linear")] object StrikeInterpolation,
              [ExcelArgument(Description = "Time Interpolation - default LinearInVariance")] object TimeInterpolation,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var atmType = ATMType.OptionalExcel<string>("ZeroDeltaStraddle");
                var wingType = WingType.OptionalExcel<string>("Simple");
                var strikeInterpType = StrikeInterpolation.OptionalExcel<string>("Linear");
                var timeInterpType = TimeInterpolation.OptionalExcel<string>("LinearInVariance");
                var expiries = ExcelHelper.ToDateTimeArray(Expiries);
                var rr = Riskies.SquareToJagged<double>();
                var bf = Flies.SquareToJagged<double>();

                if (!Enum.TryParse(wingType, out WingQuoteType wType))
                    return $"Could not parse wing quote type - {wingType}";

                if (!Enum.TryParse(atmType, out AtmVolType aType))
                    return $"Could not parse atm quote type - {atmType}";

                if (!Enum.TryParse(strikeInterpType, out Interpolator1DType siType))
                    return $"Could not parse strike interpolator type - {strikeInterpType}";

                if (!Enum.TryParse(timeInterpType, out Interpolator1DType tiType))
                    return $"Could not parse time interpolator type - {timeInterpType}";

                double[] fwds = null;
                if (FwdsOrCurve is double)
                {
                    fwds = new double[] { (double)FwdsOrCurve };
                }
                else if (FwdsOrCurve is string)
                {
                    if(!ContainerStores.GetObjectCache<IPriceCurve>().TryGetObject(FwdsOrCurve as string, out var curve))
                    {
                        return $"Could not find fwd curve with name - {FwdsOrCurve as string}";
                    }
                }
                else
                {
                    fwds = ((object[,])FwdsOrCurve).ObjectRangeToVector<double>();
                }

                var surface = new RiskyFlySurface(OriginDate, ATMVols, expiries, WingDeltas, rr, bf, fwds, wType, aType, siType, tiType, labels)
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
