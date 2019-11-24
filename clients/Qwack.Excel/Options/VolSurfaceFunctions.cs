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
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Core.Curves;
using Qwack.Dates;
using Qwack.Core.Models;
using Qwack.Excel.Curves;
using Qwack.Core.Cubes;

namespace Qwack.Excel.Options
{
    public class VolSurfaceFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<AmericanFunctions>();

        [ExcelFunction(Description = "Creates a constant vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateConstantVolSurface), IsThreadSafe = true)]
        public static object CreateConstantVolSurface(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
            [ExcelArgument(Description = "Volatility")] double Volatility,
            [ExcelArgument(Description = "Currency - default USD")] object Currency)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var ccyStr = Currency.OptionalExcel("USD");
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);

                var surface = new ConstantVolSurface(OriginDate, Volatility)
                {
                    Currency = ContainerStores.CurrencyProvider[ccyStr],
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName,
                };
                return ExcelHelper.PushToCache<IVolSurface>(surface, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a grid vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateGridVolSurface), IsThreadSafe = true)]
        public static object CreateGridVolSurface(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Asset Id")] string AssetId,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Strikes")] double[] Strikes,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "Volatilities")] double[,] Volatilities,
              [ExcelArgument(Description = "Stike Type - default Absolute")] object StrikeType,
              [ExcelArgument(Description = "Stike Interpolation - default Linear")] object StrikeInterpolation,
              [ExcelArgument(Description = "Time Interpolation - default Linear")] object TimeInterpolation,
              [ExcelArgument(Description = "Time basis - default ACT365F")] object TimeBasis,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels,
              [ExcelArgument(Description = "Currency - default USD")] object Currency,
              [ExcelArgument(Description = "Override spot lag - default none")] object SpotLag)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var ccyStr = Currency.OptionalExcel("USD");
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);


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

                if (!Enum.TryParse(timeBasis, out DayCountBasis basis))
                    return $"Could not parse time basis type - {timeBasis}";

                var surface = new GridVolSurface(OriginDate, Strikes, expiries, Volatilities.SquareToJagged(), sType, siType, tiType, basis, labels)
                {
                    Currency = ContainerStores.CurrencyProvider[ccyStr],
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName,
                };
                if (SpotLag != null && !(SpotLag is ExcelMissing))
                {
                    surface.OverrideSpotLag = new Frequency((string)SpotLag);
                }
                return ExcelHelper.PushToCache<IVolSurface>(surface, ObjectName);
            });
        }


        [ExcelFunction(Description = "Creates a grid vol surface object from RR/BF qoutes", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateRiskyFlyVolSurface), IsThreadSafe = true)]
        public static object CreateRiskyFlyVolSurface(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Asset Id")] string AssetId,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Wing deltas")] double[] WingDeltas,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "ATM Volatilities")] double[] ATMVols,
              [ExcelArgument(Description = "Risk Reversal quotes")] double[,] Riskies,
              [ExcelArgument(Description = "Butterfly quotes")] double[,] Flies,
              [ExcelArgument(Description = "Forwards or price curve object")] object FwdsOrCurve,
              [ExcelArgument(Description = "ATM vol type - default zero-delta straddle")] object ATMType,
              [ExcelArgument(Description = "Wing quote type - Simple or Market")] object WingType,
              [ExcelArgument(Description = "Stike Interpolation - default GaussianKernel")] object StrikeInterpolation,
              [ExcelArgument(Description = "Time Interpolation - default LinearInVariance")] object TimeInterpolation,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels,
              [ExcelArgument(Description = "Currency - default USD")] object Currency,
              [ExcelArgument(Description = "Override spot lag - default none")] object SpotLag)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var ccyStr = Currency.OptionalExcel("USD");
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);

                var atmType = ATMType.OptionalExcel("ZeroDeltaStraddle");
                var wingType = WingType.OptionalExcel("Simple");
                var strikeInterpType = StrikeInterpolation.OptionalExcel("GaussianKernel");
                var timeInterpType = TimeInterpolation.OptionalExcel("LinearInVariance");
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
                    if (!ContainerStores.GetObjectCache<IPriceCurve>().TryGetObject(FwdsOrCurve as string, out var curve))
                    {
                        return $"Could not find fwd curve with name - {FwdsOrCurve as string}";
                    }
                    fwds = expiries.Select(e => curve.Value.GetPriceForDate(e)).ToArray();
                }
                else
                {
                    fwds = ((object[,])FwdsOrCurve).ObjectRangeToVector<double>();
                }

                var surface = new RiskyFlySurface(OriginDate, ATMVols, expiries, WingDeltas, rr, bf, fwds, wType, aType, siType, tiType, labels)
                {
                    Currency = ContainerStores.CurrencyProvider[ccyStr],
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName,
                };
                if(SpotLag!=null && !(SpotLag is ExcelMissing))
                {
                    surface.OverrideSpotLag = new Frequency((string)SpotLag);
                }
                return ExcelHelper.PushToCache<IVolSurface>(surface, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a SABR vol surface object from RR/BF qoutes", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateSABRVolSurfaceFromRiskyFly), IsThreadSafe = true)]
        public static object CreateSABRVolSurfaceFromRiskyFly(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Asset Id")] string AssetId,
              [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
              [ExcelArgument(Description = "Wing deltas")] double[] WingDeltas,
              [ExcelArgument(Description = "Expiries")] double[] Expiries,
              [ExcelArgument(Description = "ATM Volatilities")] double[] ATMVols,
              [ExcelArgument(Description = "Risk Reversal quotes")] double[,] Riskies,
              [ExcelArgument(Description = "Butterfly quotes")] double[,] Flies,
              [ExcelArgument(Description = "Forwards or price curve object")] object FwdsOrCurve,
              [ExcelArgument(Description = "ATM vol type - default zero-delta straddle")] object ATMType,
              [ExcelArgument(Description = "Wing quote type - Simple or Market")] object WingType,
              [ExcelArgument(Description = "Time Interpolation - default LinearInVariance")] object TimeInterpolation,
              [ExcelArgument(Description = "Pillar labels (optional)")] object PillarLabels,
              [ExcelArgument(Description = "Currency - default USD")] object Currency)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var ccyStr = Currency.OptionalExcel("USD");
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);

                var atmType = ATMType.OptionalExcel("ZeroDeltaStraddle");
                var wingType = WingType.OptionalExcel("Simple");
                var timeInterpType = TimeInterpolation.OptionalExcel("LinearInVariance");
                var expiries = ExcelHelper.ToDateTimeArray(Expiries);
                var rr = Riskies.SquareToJagged<double>();
                var bf = Flies.SquareToJagged<double>();

                if (!Enum.TryParse(wingType, out WingQuoteType wType))
                    return $"Could not parse wing quote type - {wingType}";

                if (!Enum.TryParse(atmType, out AtmVolType aType))
                    return $"Could not parse atm quote type - {atmType}";

                if (!Enum.TryParse(timeInterpType, out Interpolator1DType tiType))
                    return $"Could not parse time interpolator type - {timeInterpType}";

                double[] fwds = null;
                if (FwdsOrCurve is double)
                {
                    fwds = new double[] { (double)FwdsOrCurve };
                }
                else if (FwdsOrCurve is string)
                {
                    if (!ContainerStores.GetObjectCache<IPriceCurve>().TryGetObject(FwdsOrCurve as string, out var curve))
                    {
                        return $"Could not find fwd curve with name - {FwdsOrCurve as string}";
                    }
                    fwds = expiries.Select(e => curve.Value.GetPriceForDate(e)).ToArray();
                }
                else
                {
                    fwds = ((object[,])FwdsOrCurve).ObjectRangeToVector<double>();
                }

                var surface = new SabrVolSurface(OriginDate, ATMVols, expiries, WingDeltas, rr, bf, fwds, wType, aType, tiType, labels)
                {
                    Currency = ContainerStores.CurrencyProvider[ccyStr],
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName,
                };
                return ExcelHelper.PushToCache<IVolSurface>(surface, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates an inverse pair fx vol surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(CreateInvertedFxSurface), IsThreadSafe = true)]
        public static object CreateInvertedFxSurface(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Input surface name")] string InputSurface)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(InputSurface, out var volSurface) && volSurface.Value is IATMVolSurface atmSurface)
                {

                    return ExcelHelper.PushToCache<IVolSurface>(new InverseFxSurface(ObjectName, atmSurface, ContainerStores.CurrencyProvider), ObjectName);
                }

                return $"Vol surface {InputSurface} not found in cache or could not be case to ATM Surface";
            });
        }

        [ExcelFunction(Description = "Gets a volatility for a delta strike from a vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GetVolForDeltaStrike), IsThreadSafe = true)]
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

        [ExcelFunction(Description = "Gets a volatility for an absolute strike from a vol surface object", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GetVolForAbsoluteStrike), IsThreadSafe = true)]
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

        [ExcelFunction(Description = "Creates a CDF from a vol surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GenerateCDF), IsThreadSafe = true)]
        public static object GenerateCDF(
             [ExcelArgument(Description = "Output interpolator name")] string ObjectName,
             [ExcelArgument(Description = "Volsurface name")] string VolSurface,
             [ExcelArgument(Description = "Expiry date")] DateTime ExpiryDate,
             [ExcelArgument(Description = "Forward")] double Forward,
             [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(VolSurface, out var volSurface))
                {
                    var interpolator = volSurface.Value.GenerateCDF(NumberOfSamples, ExpiryDate, Forward);
                    return ExcelHelper.PushToCache(interpolator, ObjectName);
                }

                return $"Vol surface {VolSurface} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates a PDF from a vol surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GeneratePDF), IsThreadSafe = true)]
        public static object GeneratePDF(
             [ExcelArgument(Description = "Output interpolator name")] string ObjectName,
             [ExcelArgument(Description = "Volsurface name")] string VolSurface,
             [ExcelArgument(Description = "Expiry date")] DateTime ExpiryDate,
             [ExcelArgument(Description = "Forward")] double Forward,
             [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(VolSurface, out var volSurface))
                {
                    var interpolator = volSurface.Value.GeneratePDF(NumberOfSamples, ExpiryDate, Forward);
                    return ExcelHelper.PushToCache(interpolator, ObjectName);
                }

                return $"Vol surface {VolSurface} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates a PDF from a smile interpolator", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GeneratePDFFromInterpolator), IsThreadSafe = true)]
        public static object GeneratePDFFromInterpolator(
            [ExcelArgument(Description = "Output interpolator name")] string ObjectName,
            [ExcelArgument(Description = "Input interpolator name")] string VolInterpolator,
            [ExcelArgument(Description = "Expiry as year fraction")] double ExpiryYearFraction,
            [ExcelArgument(Description = "Forward")] double Forward,
            [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IInterpolator1D>().TryGetObject(VolInterpolator, out var smile))
                {
                    var interpolator = smile.Value.GeneratePDF(NumberOfSamples, ExpiryYearFraction, Forward);
                    return ExcelHelper.PushToCache(interpolator, ObjectName);
                }

                return $"Interpolator {VolInterpolator} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates a CDF from a smile interpolator", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GenerateCDFFromInterpolator), IsThreadSafe = true)]
        public static object GenerateCDFFromInterpolator(
            [ExcelArgument(Description = "Output interpolator name")] string ObjectName,
            [ExcelArgument(Description = "Input interpolator name")] string VolInterpolator,
            [ExcelArgument(Description = "Expiry as year fraction")] double ExpiryYearFraction,
            [ExcelArgument(Description = "Forward")] double Forward,
            [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IInterpolator1D>().TryGetObject(VolInterpolator, out var smile))
                {
                    var interpolator = smile.Value.GenerateCDF(NumberOfSamples, ExpiryYearFraction, Forward);
                    return ExcelHelper.PushToCache(interpolator, ObjectName);
                }

                return $"Interpolator {VolInterpolator} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates a composite smile from a vol surface and an fx vol surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GenerateCompoSmile), IsThreadSafe = true)]
        public static object GenerateCompoSmile(
             [ExcelArgument(Description = "Output interpolator name")] string ObjectName,
             [ExcelArgument(Description = "Volsurface name")] string VolSurface,
             [ExcelArgument(Description = "Fx Volsurface name")] string FxVolSurface,
             [ExcelArgument(Description = "Expiry date")] DateTime ExpiryDate,
             [ExcelArgument(Description = "Forward")] double Forward,
             [ExcelArgument(Description = "Fx Forward")] double FxForward,
             [ExcelArgument(Description = "Correlation")] double Correlation,
             [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(VolSurface, out var volSurface))
                {
                    if (ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(FxVolSurface, out var volSurfaceFx))
                    {
                        var interpolator = volSurface.Value.GenerateCompositeSmile(volSurfaceFx.Value, NumberOfSamples, ExpiryDate, Forward, FxForward, Correlation);
                        return ExcelHelper.PushToCache(interpolator, ObjectName);
                    }
                    else
                        return $"Fx vol surface {FxVolSurface} not found in cache";
                }
                else
                    return $"Vol surface {VolSurface} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates a composite surface from a vol surface and an fx vol surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(GenerateCompoSurface), IsThreadSafe = true)]
        public static object GenerateCompoSurface(
             [ExcelArgument(Description = "Output surface name")] string ObjectName,
             [ExcelArgument(Description = "Asset-fx model name")] string AssetFxModel,
             [ExcelArgument(Description = "AssetId")] string AssetId,
             [ExcelArgument(Description = "FxPair")] string FxPair,
             [ExcelArgument(Description = "Correlation")] double Correlation,
             [ExcelArgument(Description = "Number of samples")] int NumberOfSamples)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                if (ContainerStores.GetObjectCache<IAssetFxModel>().TryGetObject(AssetFxModel, out var model))
                {
                    if (!model.Value.FundingModel.VolSurfaces.ContainsKey(FxPair))
                        throw new Exception($"No vol surface found in model for fx pair {FxPair}");
                    model.Value.GetVolSurface(AssetId);

                    var surface = model.Value.GenerateCompositeSurface(AssetId, FxPair, NumberOfSamples, Correlation, true);
                    return ExcelHelper.PushToCache<IVolSurface>(surface, ObjectName);
                }
                else
                    return $"Model {AssetFxModel} not found in cache";
            });
        }

        [ExcelFunction(Description = "Turns a risky/fly surface into a cube of quotes", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(SurfaceToCube), IsThreadSafe = true)]
        public static object SurfaceToCube(
            [ExcelArgument(Description = "Surface name")] string SurfaceName,
            [ExcelArgument(Description = "Output cube name")] string CubeName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var surface = ContainerStores.GetObjectCache<IVolSurface>().GetObjectOrThrow(SurfaceName, $"Vol surface {SurfaceName} not found");
                var rrbf = (RiskyFlySurface)surface.Value;
                var cube = rrbf.ToCube();
                return RiskFunctions.PushCubeToCache(cube, CubeName);
            });
        }

        [ExcelFunction(Description = "Reconstructs a risky/fly surface from a cube of quotes", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(SurfaceFromCube), IsThreadSafe = true)]
        public static object SurfaceFromCube(
            [ExcelArgument(Description = "Output Surface name")] string SurfaceName,
            [ExcelArgument(Description = "Cube name")] string CubeName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Currency")] string Currency,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Stike Interpolation - default GaussianKernel")] object StrikeInterpolation,
            [ExcelArgument(Description = "Time Interpolation - default LinearInVariance")] object TimeInterpolation)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(CubeName, $"Cube {CubeName} not found");
                var strikeInterpType = StrikeInterpolation.OptionalExcel("GaussianKernel");
                var timeInterpType = TimeInterpolation.OptionalExcel("LinearInVariance");

                if (!Enum.TryParse(strikeInterpType, out Interpolator1DType siType))
                    return $"Could not parse strike interpolator type - {strikeInterpType}";
                if (!Enum.TryParse(timeInterpType, out Interpolator1DType tiType))
                    return $"Could not parse time interpolator type - {timeInterpType}";

                var rrbf = RiskyFlySurface.FromCube(cube.Value, BuildDate, siType, tiType);
                rrbf.AssetId = AssetId;
                rrbf.Name = AssetId;
                if (!string.IsNullOrWhiteSpace(Currency))
                    rrbf.Currency = ContainerStores.CurrencyProvider.GetCurrency(Currency);

                return ExcelHelper.PushToCache<IVolSurface>(rrbf, SurfaceName);
            });
        }

        [ExcelFunction(Description = "Extracts quotes from a risky/fly surface", Category = CategoryNames.Volatility, Name = CategoryNames.Volatility + "_" + nameof(SurfaceToQuotes), IsThreadSafe = true)]
        public static object SurfaceToQuotes(
            [ExcelArgument(Description = "Surface name")] string SurfaceName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var surface = ContainerStores.GetObjectCache<IVolSurface>().GetObjectOrThrow(SurfaceName, $"Vol surface {SurfaceName} not found");
                var rrbf = (RiskyFlySurface)surface.Value;
                return rrbf.DisplayQuotes();
            });
        }
    }
}
