using System;
using System.Threading;
using Cactus.Drivers;
using Cactus.Infrastructure;
using Should;
using By = OpenQA.Selenium.By;
using How = Cactus.Drivers.PageObject.How;

namespace Cactus.PageLogic
{
    /// <summary>
    /// This Class is for use of common methods.
    ///    Possible sources, Navigation.
    /// </summary>
    public abstract class BasePageObject : IDisposable
    {

        private UxTestingLogger Log;
        public enum FeatureType
        {
            Asset,
            Broadcast,
            Case,
            Employee,
            Organization,
            Site,
            Solution
        }

        public enum ActionType
        {
            CreateNew,
            Query
        }

        #region Common Controls regarding HTML
        public readonly Control Body = new Control(How.TagName, "body");
        public readonly Control Panel = new Control(How.ClassName, "panel-body");


        #endregion

        #region Common ToolBar Navigation


        #endregion

        #region Common Button Clicks

        #endregion

        #region Common Error Message Logic

        /// <summary>
        ///   Pass this method a string (e.g. the endpoint of a URI of an MVC app)
        ///   Method verifies that the current browser URL contains the string and returns
        ///   true or false depending on the state
        /// </summary>
        /// <param name="urlString"></param>
        /// <returns>Does not Assert</returns>
        public bool CurrentUrlContains(string urlString)
        {
            var currentUrl = Engine.WebDriver.Url.ToLowerInvariant();
            return currentUrl.Contains(urlString.ToLowerInvariant());
        }
        
        /// <summary>
        ///   Asserts that the current browser URL contains the string
        /// </summary>
        /// <param name="urlPartialString"></param>
        public void AssertCurrentUrlContains(string urlPartialString)
        {
            
            NunitExtensions.AssertCurrentUrlContains(urlPartialString);
        }

        /// <summary>
        ///   Asserts that the currently display page is not a YSOD/500 error
        /// </summary>
        public static void VerifyIsNot500Error()
        {
            Engine.Is500Error.ShouldBeFalse();
        }

     #endregion

        #region Common Methods


        public void Pause(int millisecondsToPause = 100)
        {
            Thread.Sleep(millisecondsToPause);
        }

        public void Refresh()
        {
            Engine.RefreshPage();
        }

        public Control GoogleAnalyticsShouldHaveRunSuccessfully()
        {
            // When GA runs, it adds a <script> tag in front of all the other tags that points to http[s]://.../ga.js
            var scriptElements = Engine.FindElements(How.Src, "http://www.google-analytics.com/ga.js");
            if (scriptElements == null || scriptElements.Count == 0)
            {
                return null;
            }
            foreach (var script in scriptElements)
            {
                if (!string.IsNullOrEmpty(script.GetAttribute("src")))
                    return new Control(script);
            }
            return new Control(scriptElements[0]);
        }

        public bool GoogleAnalyticsIdShouldBe(string analyticsId)
        {
            var pageSource = Engine.GetEntirePageSource;
            return pageSource.Contains(analyticsId);
        }

        public string GetCurrentUrl()
        {
            return Engine.GetCurrentUrl;
        }

        public bool IsWebSiteError500or403
        {
            get
            {
                return Engine.Is403Error || Engine.Is500Error;
            }
        }

        public bool Is403Error
        {
            get
            {
                return Engine.Is403Error;
            }
        }

        public bool Is404Error
        {
            get
            {
                return Engine.Is404Error;
            }
        }

        public bool Is500Error
        {
            get
            {
                return Engine.Is500Error;
            }
        }

        public void ScrollToBottomOfPage()
        {
            Engine.ScrollToBottomOfPage();
        }

        #endregion

        #region disposal

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                //
            }

            // Free any unmanaged objects here. 
            //
            disposed = true;
        }
        #endregion
    }
}
