using System;

namespace Cactus.Infrastructure
{
    public class UxTestingLogger :  IDisposable
    {

        const string Debug = "DEBUG";
        const string Info = "INFO";
        const string Warning = "WARN";
        const string Error = "ERROR";
        const string Fatal = "FATAL";

        public void LogDebug(string message)
        {
            log(Debug, message);
        }

        public void LogDebug(string message, params object[] parameters)
        {
            log(Debug, message, parameters);
        }

        public void LogInfo(string message, params object[] parameters)
        {
            log(Info, message, parameters);
        }

        public void LogInfo(string message)
        {
            log(Info, message);
        }

        public void LogWarn(string message)
        {
            log(Warning, message);
        }
        public void LogWarn(string message, params object[] parameters)
        {
            log(Warning, message, parameters);
        }

        public void LogError(string message)
        {
            log(Error, message);
        }

        public void LogError(string message, params object[] parameters)
        {
            log(Error, message, parameters);
        }

        public void LogFatal(string message)
        {
            log(Fatal, message);
        }

        public void LogFatal(string message, params object[] parameters)
        {
            log(Fatal, message, parameters);
        }

        public void LogError(string message, Exception exception)
        {
            log(Error, "{0}\n\tException Details: {1}", message, exception == null ? "" : "\n\t\t" + String.Join("\n\t\t", exception.ToString().Split('\n')));
        }

        public IDisposable Push(string context)
        {
            return this;
        }

        public void LogException(Exception exception)
        {
            if (exception == null)
            {
                LogError("Weird: we were asked to log a null exception.");
                return;
            }
            LogError("EXCEPTION", exception);
        }

        public void Dispose() { }

        static void log(string level, string message)
        {
            if (message.StartsWith("WaitFor"))
            {
                Console.WriteLine(string.Format("{0} {1}", "#", message));
            }
            else if (message.StartsWith("PASS") || message.StartsWith("FAIL") || message.StartsWith("-"))
            {
                Console.WriteLine(string.Format("{0} {1}", "~", message));
            }
            else
            {
                Console.WriteLine(string.Format("{0}: {1}", level, message));
            }

        }

        static void log(string level, string message, params object[] parameters)
        {
            log(level, string.Format(message,parameters) );
        }
    }
}
