using System;
using System.Collections.Generic;
using System.Text;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Qwack.Utils
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddQwackLogging(this IServiceCollection serviceCollection)
        {
            
            var logPath = Environment.GetEnvironmentVariable("QWACKLOGPATH");
            if(!string.IsNullOrEmpty(logPath))
            {
                serviceCollection.AddLogging(lb => lb.AddFile(new FileLoggerContext(logPath, "qwack.log")));
            }
            else
            {
                serviceCollection.AddLogging();
            }
            return serviceCollection;
        }
    }
}
