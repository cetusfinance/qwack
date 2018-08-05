using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Core.Instruments.Futures;
using Qwack.Dates;

namespace Qwack.Providers.Json
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFutureSettingsFromJson(this IServiceCollection serviceCollection, string fileName) 
            => serviceCollection.AddSingleton<IFutureSettingsProvider>(sp =>
                {
                    var calendars = sp.GetRequiredService<ICalendarProvider>();
                    return new FutureSettingsFromJson(calendars, fileName);
                });
    }
}
