using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;

namespace Qwack.Providers.Json
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFutureSettingsFromJson(this IServiceCollection serviceCollection, string fileName)
            => serviceCollection.AddSingleton<IFutureSettingsProvider>(sp =>
                {
                    var calendars = sp.GetRequiredService<ICalendarProvider>();
                    return new FutureSettingsFromJson(calendars, fileName, sp.GetRequiredService<ILoggerFactory>());
                });

        public static IServiceCollection AddCalendarsFromJson(this IServiceCollection serviceCollection, string fileName) => serviceCollection.AddSingleton<ICalendarProvider>(CalendarsFromJson.Load(fileName));

        public static IServiceCollection AddCurrenciesFromJson(this IServiceCollection serviceCollection, string fileName)
            => serviceCollection.AddSingleton<ICurrencyProvider>(sp =>
            {
                var calendars = sp.GetRequiredService<ICalendarProvider>();
                return new CurrenciesFromJson(calendars, fileName, sp.GetRequiredService<ILoggerFactory>().CreateLogger<CurrenciesFromJson>());
            });
    }
}
