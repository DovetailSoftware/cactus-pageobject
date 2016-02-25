using System;

namespace Cactus.Infrastructure
{
    public interface ILoggingService
    {
        ILogger LoggerFor(Type type);
    }
}
