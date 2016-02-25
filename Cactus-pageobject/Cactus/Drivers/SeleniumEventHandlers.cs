using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Cactus.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Events;

namespace Cactus.Drivers
{
    public class SeleniumEventHandlers
    {
        readonly ILogger _log;
        public ILogger Log { get { return _log; } }
        public SeleniumEventHandlers()
        {
            _log = LogManager.GetLogger(null);
        }

        public void firingDriver_ExceptionThrown(object sender, WebDriverExceptionEventArgs e)
        {
            //if (e.ThrownException.GetType() == typeof (NoSuchElementException)) 
            //{
            //    _log.Exception(e.ThrownException);       
            //}

            if (e.ThrownException.Message.Contains("sending an HTTP request to the remote WebDriver server") ||
                e.ThrownException.Message.Contains("No session ID specified"))
            {
                return; //ignore these. do not screenshot.
            }


            if (e.ThrownException.GetType() == typeof(NoSuchElementException) ||
                e.ThrownException.GetType() == typeof(NoSuchWindowException) ||
                e.ThrownException.GetType() == typeof(StaleElementReferenceException) ||
                e.ThrownException.GetType() == typeof(NoAlertPresentException) ||
                e.ThrownException.GetType() == typeof(InvalidOperationException) ||
                e.ThrownException is ThreadAbortException)
            {
                return; //ignore these.
            }

            if (e.ThrownException.GetType() == typeof(ArgumentNullException) && e.ThrownException.Message.Contains("by cannot be null"))
            {
                return; //ignore these.
            }

            Support.ScreenShot();

            //OpenQA.Selenium.UnhandledAlertException: unexpected alert open
            if (e.ThrownException.GetType() == typeof(UnhandledAlertException))
            {
                Engine.AlertAccept();
                return;
            }

            _log.Error(getReflectionUsage(), e.ThrownException);
        }

        public void firingDriver_AlertExceptionThrown(object sender, WebDriverExceptionEventArgs e)
        {
            //OpenQA.Selenium.UnhandledAlertException: unexpected alert open
            if (e.ThrownException.GetType() == typeof(UnhandledAlertException))
            {
                Support.ScreenShot();
                _log.Error(getReflectionUsage(), e.ThrownException);
                Engine.AlertAccept();
            }
        }
        public void firingDriver_FindingElement(object sender, FindElementEventArgs e)
        {
            _log.Debug(
                $"FindingElement from {(e.Element == null ? "IWebDriver " : "IWebElement ")} {e.FindMethod.ToString()}");
        }

        public void firingDriver_FindElementCompleted(object sender, FindElementEventArgs e)
        {
            _log.Debug(
                $"FindElementCompleted from {(e.Element == null ? "IWebDriver " : "IWebElement ")} {e.FindMethod.ToString()}");
        }

        public void firingDriver_ElementClicking(object sender, WebElementEventArgs e)
        {
            _log.Debug("Clicking");
        }

        public void firingDriver_ElementClicked(object sender, WebElementEventArgs e)
        {
            _log.Debug("Clicked");
        }

        public void firingDriver_Navigating(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug($"Navigating {e.Url}");
        }

        public void firingDriver_Navigated(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug($"Navigated {e.Url}");
        }

        public void firingDriver_NavigatingBack(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug("Navigating back");
        }

        public void firingDriver_NavigatedBack(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug("Navigated back");
        }

        public void firingDriver_NavigatingForward(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug("Navigating forward");
        }

        public void firingDriver_NavigatedForward(object sender, WebDriverNavigationEventArgs e)
        {
            _log.Debug("Navigated forward");
        }

        private static string getReflectionUsage(int maxReflection = 5)
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
