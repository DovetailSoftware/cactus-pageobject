using System;
using System.Threading.Tasks;

namespace Cactus.Infrastructure
{
    public interface ILogger
    {
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }

        Task Info(string message, params object[] parameters);
        Task Info(string message);
        Task Info(object message);

        Task Warn(string message, params object[] parameters);
        Task Warn(string message);

        Task Debug(string message);
        Task Debug(object message);
        Task Debug(string message, params object[] parameters);
        Task Debug(string message, Exception exception);

        Task Error(string message);
        Task Error(string message, params object[] parameters);
        Task Error(string message, Exception exception);

        Task Fatal(string message);
        Task Fatal(string message, params object[] parameters);
        Task Fatal(string message, Exception exception);

        Task Exception(Exception exception);
    }
}
