using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using Cactus.Infrastructure;
using Cactus.Infrastructure.NStatsD;

namespace Cactus.Drivers
{
    public class PerformanceTimer : IDisposable
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        readonly Action<TimeSpan> _callback;

        public PerformanceTimer()
        {
            _stopwatch.Start();
        }

        public PerformanceTimer(Action<TimeSpan> callback)
            : this()
        {
            _callback = callback;
        }

        public static PerformanceTimer Start(Action<TimeSpan> callback)
        {
            return new PerformanceTimer(callback);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            if (_callback != null)
                _callback(Result);
        }

        public TimeSpan Result
        {
            get { return _stopwatch.Elapsed; }
        }

        public static void ShowTimeResult(TimeSpan timeSpan)
        {
            Debug.WriteLine("Process took {0} milliseconds", timeSpan.TotalMilliseconds);
        }

        public static void LogTimeResult(string messagePart, TimeSpan timeSpan)
        {
            LogInfo(string.Format("{0} took {1} milliseconds", messagePart, timeSpan.TotalMilliseconds));
            try
            {
                var methodCalled = GetReflectionUsage();
                var key = "testing.ui.method_triggered.machine." + Environment.MachineName.ToLower() + "." +
                          methodCalled;
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["NstatUsage"]))
                {
                     Client.Current.Increment(key);
                     Client.Current.Timing(key, timeSpan.TotalMilliseconds);
                }   
            }
            catch (Exception ex)
            {
                var log4Net = new UxTestingLogger();
                log4Net.LogError(ex.Message);
            }

        }

        public static void LogTimeResult(string messagePart, TimeSpan timeSpan, string fullUrl)
        {
            var urlPart = string.Empty;
            if (fullUrl.Length > 0)
            {
                LogStatsD(urlPart, timeSpan);
            }
        }

        private static void LogInfo(string message)
        {
            var log4Net = new UxTestingLogger();
            log4Net.LogInfo(message);
        }

        private static void LogStatsD(string urlPart, TimeSpan timeSpan)
        {
            try
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["NstatUsage"]))
                {
                    var url = Regex.Replace(urlPart.ToLower(), "[^A-Za-z0-9 _]", "_");
                    var key = "testing.ui.page_load.machine." + Environment.MachineName.ToLower() + ".url." + url;
                    Client.Current.Timing(key, timeSpan.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                var log4Net = new UxTestingLogger();
                log4Net.LogError(ex.Message);
            }
        }

        private static string GetReflectionUsage(int maxReflection = 5)
        {
            try
            {
                var stackTrace = new StackTrace();
                // Display the most recent function call.
                if (stackTrace.GetFrame(0) == null)
                {
                    // exit out, as something has gone wrong.
                    return "";
                }
                var methodName = "";
                var className = "";
                int counter = 0;
                foreach (StackFrame stackFrame in stackTrace.GetFrames())
                {
                    if (counter > maxReflection)
                    {
                        break;
                    }
                    if (counter > 3)
                    {
                        // get calling method name by level
                        MethodBase method = stackFrame.GetMethod();
                        if (methodName != method.Name || className != method.ReflectedType.Name)
                        {
                            methodName = method.Name;
                            className = method.ReflectedType.Name;
                            return "class." + className + ".method." + methodName;
                        }
                    }
                    counter++;
                }
            }
            catch (Exception)
            {
                return "";
                //ignore
            }
            return "";
        }
    }
}

