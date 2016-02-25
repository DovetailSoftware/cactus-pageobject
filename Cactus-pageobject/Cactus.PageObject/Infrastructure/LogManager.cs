using System;

namespace Cactus.Infrastructure
{
    public class LogManager
    {
        public static ILoggingService LoggingService { get; set; }

        public static ILogger GetLogger(Type type)
        {
            return LoggingService == null ? new UxTestingLogger() : LoggingService.LoggerFor(type);
        }
    }
}
