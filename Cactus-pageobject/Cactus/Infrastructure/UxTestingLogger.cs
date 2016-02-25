using System;
using System.Threading.Tasks;
using Cactus.Drivers;

namespace Cactus.Infrastructure
{
    public class UxTestingLogger : ILogger, IDisposable
    {
        const string DebugText = "DEBUG";
        const string InfoText = "INFO";
        const string WarningText = "WARN";
        const string ErrorText = "ERROR";
        const string FatalText = "FATAL";

        public bool IsInfoEnabled { get { return true; } }
        public bool IsWarnEnabled { get { return true; } }
        public bool IsDebugEnabled { get { return true; } }
        public bool IsErrorEnabled { get { return true; } }
        public bool IsFatalEnabled { get { return true; } }

        public Task Info(object message)
        {
            return Task.Run(() => log(InfoText, message));
        }

        public Task Info(string message)
        {
            return Task.Run(() => log(InfoText, message));
        }

        public Task Info(string message, params object[] parameters)
        {
            return Task.Run(() => log(InfoText, message, parameters));
        }

        public Task Warn(string message)
        {
            return Task.Run(() => log(WarningText, message));
        }

        public Task Warn(string message, params object[] parameters)
        {
            return Task.Run(() => log(WarningText, message, parameters));
        }

        public Task Debug(object message)
        {
            return Task.Run(() => log(DebugText, message));
        }

        public Task Debug(string message)
        {
            return Task.Run(() => log(DebugText, message));
        }

        public Task Debug(string message, params object[] parameters)
        {
            return Task.Run(() => log(DebugText, message, parameters));
        }

        public Task Debug(string message, Exception exception)
        {
            return Task.Run(() => log(DebugText, "{0}\n\tException Details: {1}", message, exception == null ? "" : "\n\t\t" + exception.ToString().Split('\n').Join("\n\t\t")));
        }

        public Task Error(string message)
        {
            return Task.Run(() => log(ErrorText, message));
        }

        public Task Error(string message, params object[] parameters)
        {
            return Task.Run(() => log(ErrorText, message, parameters));
        }

        public Task Error(string message, Exception exception)
        {
            return Task.Run(() => log(ErrorText, "{0}\n\tException Details: {1}", message, exception == null ? "" : "\n\t\t" + exception.ToString().Split('\n').Join("\n\t\t")));
        }

        public Task Fatal(string message)
        {
            return Task.Run(() => log(FatalText, message));
        }

        public Task Fatal(string message, params object[] parameters)
        {
            return Task.Run(() => log(FatalText, message, parameters));
        }

        public Task Fatal(string message, Exception exception)
        {
            return Task.Run(() => log(FatalText, "{0}\n\tException Details: {1}", message, exception == null ? "" : "\n\t\t" + exception.ToString().Split('\n').Join("\n\t\t")));
        }

        public IDisposable Push(string context)
        {
            return this;
        }

        public Task Exception(Exception exception)
        {
            return Task.Run(() =>
            {
                if (exception == null)
                {
                    Error("Weird: we were asked to log a null exception.");
                    return;
                }

                Error("EXCEPTION " +
                      string.Format("Message --- {0}{1}{0}Source --- {2}{0}StackTrace --- {0}{3}{0}",
                          Environment.NewLine,
                          exception.Message,
                          exception.Source,
                          exception.StackTrace));
            });
        }

        public void Dispose() { }

        static void log(string level, object message)
        {
            log(level, message.ToString());
        }

        static void log(string level, string message)
        {
            if (message.StartsWith("Fiddler") && level == InfoText)
            {
                Console.WriteLine($"{"#"} {message}");
            }
            else if (message.StartsWith("WaitFor"))
            {
                Console.WriteLine($"{"#"} {message}");
            }
            else if (message.StartsWith("PASS") || message.StartsWith("FAIL") || message.StartsWith("-"))
            {
                Console.WriteLine($"{"~"} {message}");
            }
            else
            {
                Console.WriteLine($"{level}: {message}");
            }

        }

        static void log(string level, string message, params object[] parameters)
        {
            log(level, string.Format(message, parameters));
        }


    }
}
