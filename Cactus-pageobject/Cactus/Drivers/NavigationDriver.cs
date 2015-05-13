using System;
using System.Threading;
using Cactus.Infrastructure;

namespace Cactus.Drivers
{
    public class NavigationDriver
    {
        private readonly UxTestingLogger _log;

        public NavigationDriver(UxTestingLogger log)
        {
            _log = log ?? new UxTestingLogger();
        }

        public string DefaultUser { get; private set; }


        public void NavigateTo(string url)
        {
            GoTo(url, true);
        }

        public void Refresh()
        {
            var url = Engine.GetCurrentUrl.TrimEnd('#');
            GoTo(url, true);
        }
   

        public void GoTo(string url, bool autoLogin)
        {
            _log.LogInfo("Navigating to: " + url);
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("Navigate To Url " + url, timer, url)))
            {
                if (url.StartsWith("/"))
                {
                    url = @"http://localhost" + url;
                }

                try
                {
                    Engine.GoToUrl(url);
                }
                catch (Exception ex)
                {
                    _log.LogError(string.Format("Failed to navigate to url '{0}'",url), ex);
                }
            }
            if (url.ToLower().Contains("logout"))
            {
                Support.WaitForUrlToContain("login");
            }
            // log if URL is wrong or redirected to wrong place.
            if (!Support.VerifyCurrentUrlContains(url))
            {
                Thread.Sleep(500);
                if (!url.Contains("logout"))
                {
                    _log.LogInfo("URL Navigated to: " + url + " but URL is actually " + Engine.GetCurrentUrl);
                    if (Support.VerifyCurrentUrlContains("login"))
                    {
                        _log.LogWarn("URL Navigated login and will autoLogin with default user/pass " + Engine.GetCurrentUrl);
                    }
                }
            }

            // AutoLogin procedure
            if (autoLogin && Support.VerifyCurrentUrlContains("login"))
            {
                _log.LogInfo("Auto logging in -NavigationDriver");
                using (PerformanceTimer.Start(
                    timer => PerformanceTimer.LogTimeResult("AutoLogin", timer)))
                {
                    //input your login logic.
                }
            }
            else
            {
                Thread.Sleep(20); // here so we can wait for page initiation.
                Support.WaitForPageReadyState();
            }
        }
    }
}
