using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Qwack.Utils
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddQwackLogging(this IServiceCollection serviceCollection)
        {

            var logPath = Environment.GetEnvironmentVariable("QWACKLOGPATH");
            if (!string.IsNullOrEmpty(logPath))
            {
                serviceCollection.AddLogging(lb => lb.AddFile(lo =>
                {
                    lo.BasePath = logPath;
                }));
            }
            else
            {
                serviceCollection.AddLogging();
            }
            return serviceCollection;
        }
    }
}
