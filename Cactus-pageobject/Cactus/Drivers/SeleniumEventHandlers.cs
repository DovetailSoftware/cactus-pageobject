using OpenQA.Selenium.Support.Events;
using Cactus.Infrastructure;
using OpenQA.Selenium;

namespace Cactus.Drivers
{
    public class SeleniumEventHandlers
    {
        readonly UxTestingLogger _log;
        public UxTestingLogger Log { get { return _log; } }
        public SeleniumEventHandlers()
        {
            _log = new UxTestingLogger();
        }

        public void firingDriver_ExceptionThrown(object sender, WebDriverExceptionEventArgs e)
        {
            if (e.ThrownException.Message.Contains("sending an HTTP request to the remote WebDriver server") ||
                e.ThrownException.Message.Contains("No session ID specified"))
                return; //ignore these. do not screenshot.


            if (e.ThrownException.GetType() == typeof (NoSuchElementException) ||
                e.ThrownException.GetType() == typeof (StaleElementReferenceException) ||
                e.ThrownException.GetType() == typeof (NoAlertPresentException))
                return; //ignore these.

            //OpenQA.Selenium.UnhandledAlertException: unexpected alert open
            if (e.ThrownException.GetType() == typeof (UnhandledAlertException))
            {
                Engine.AlertGoAway();
                return;
            }

            _log.LogException(e.ThrownException);
            Support.ScreenShot();
        }

        public void firingDriver_FindingElement(object sender, FindElementEventArgs e)
        {
            _log.LogDebug(string.Format("FindingElement from {0} {1}",
                e.Element == null ? "IWebDriver " : "IWebElement ",
                e.FindMethod.ToString()));
        }

        public void firingDriver_FindElementCompleted(object sender, FindElementEventArgs e)
        {
            _log.LogDebug(string.Format("FindElementCompleted from {0} {1}",
                e.Element == null ? "IWebDriver " : "IWebElement ",
                e.FindMethod.ToString()));
        }

        public void firingDriver_ElementClicking(object sender, WebElementEventArgs e)
        {
            _log.LogDebug("Clicking");
        }

        public void firingDriver_ElementClicked(object sender, WebElementEventArgs e)
        {
            _log.LogDebug("Clicked");
        }

        public void firingDriver_Navigating(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug(string.Format("Navigating {0}", e.Url));
        }

        public void firingDriver_Navigated(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug(string.Format("Navigated {0}", e.Url));
        }

        public void firingDriver_NavigatingBack(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug("Navigating back");
        }

        public void firingDriver_NavigatedBack(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug("Navigated back");
        }

        public void firingDriver_NavigatingForward(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug("Navigating forward");
        }

        public void firingDriver_NavigatedForward(object sender, WebDriverNavigationEventArgs e)
        {
            _log.LogDebug("Navigated forward");
        }

    }
}
