using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Xml.Serialization;
using Cactus.Infrastructure;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.Events;
using OpenQA.Selenium.Support.UI;
using By = OpenQA.Selenium.By;

namespace Cactus.Drivers
{
    /// <summary>
    ///     This is the main Wrapper Class for Selenium usage.
    ///     sample code: Engine.WebDriver.XXXX  Engine.GetCurrentUrl  , Engine.Execute ,   Engine.GetAllCookies
    /// </summary>
    [SuppressMessage("ReSharper", "ArrangeStaticMemberQualifier")]
    public class Engine : IDisposable
    {
        public const SupportedBrowserType DefaultBrowserType = SupportedBrowserType.Chrome;
        static string _lastMessage = String.Empty;
        //public static IWebDriver WebDriver;
        public static EventFiringWebDriver WebDriver;
        public static bool Status = true;
        public static string GlobalConfigurationFilePath = @"c:\SeleniumDriverConfig.xml";
        public static dynamic CurrentControl;
        public static SupportedBrowserType Browser;
        static string _driverType;
        static readonly object _lock = new object();
        public string Url = String.Empty;
        public string Version;
        public static string BaseUrl { get; set; }
        static UxTestingLogger _log;
        public static UxTestingLogger Log { get { return _log; } }

        public Engine()
        {
            _log = new UxTestingLogger();
        }
        ~Engine()
        {
            // AppDomain.CurrentDomain.FirstChanceException -= ExceptionWatcher;
        }

        /// <summary>
        ///     Initialize the WebDriver
        /// </summary>
        public static void InitializeBrowserInstance()
        {
            _log = new UxTestingLogger();
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("Initialize of Browser", timer)))
            {
                try
                {
                    if (Engine.WebDriver == null)
                    {
                        startAsBrowser(GetBrowserType);
                    }
                }
                catch
                {
                    startAsBrowser(GetBrowserType);
                }
            }
            if (WebDriver != null)
            {
                WebDriver.ExceptionThrown += new SeleniumEventHandlers().firingDriver_ExceptionThrown;
                WebDriver.Navigating += new SeleniumEventHandlers().firingDriver_Navigating;
            }
        }

        /// <summary>
        ///     Initialize the WebDriver
        /// </summary>
        public static void InitializeBrowserInstance(SupportedBrowserType browserType)
        {
            _log = new UxTestingLogger();
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("Initialize of Browser", timer)))
            {
                try
                {
                    if (Engine.WebDriver == null)
                    {
                        startAsBrowser(browserType);
                    }
                }
                catch
                {
                    // startAsBrowser(browserType);
                }
            }
            if (Engine.WebDriver != null)
            {
                Engine.WebDriver.ExceptionThrown += new SeleniumEventHandlers().firingDriver_ExceptionThrown;
                Engine.WebDriver.Navigating += new SeleniumEventHandlers().firingDriver_Navigating;
            }
        }

        public static void EnsureBrowserIsLaunched()
        {
            try
            {
                if (Engine.WebDriver == null)
                {
                    startAsBrowser(GetBrowserType);
                    if (WebDriver != null)
                    {
                        Engine.WebDriver.ExceptionThrown += new SeleniumEventHandlers().firingDriver_ExceptionThrown;
                        Engine.WebDriver.Navigating += new SeleniumEventHandlers().firingDriver_Navigating;
                    }
                    return;
                }

                var test = GetCurrentUrl;  // if this has a value, no need to get new browser.
                if (test == null)
                    startAsBrowser(GetBrowserType);
            }
            catch
            {
                startAsBrowser(GetBrowserType);
            }
        }

        public static void CloseWindow()
        {
            Engine.WebDriver.Close();
        }

        /// <summary>
        ///     Shutdown the Engine/ webdriver.
        /// </summary>
        public void Dispose()
        {
            ShutDown();
        }

        /// <summary>
        /// Delete all cookies from the Instance.
        /// </summary>
        public static void DeleteAllCookies()
        {
            Engine.WebDriver.Manage().Cookies.DeleteAllCookies();
        }

        public static IEnumerable<Cookie> GetAllCookies()
        {
            return Engine.WebDriver.Manage().Cookies.AllCookies;
        }

        public static void LogAllCookies()
        {
            foreach (var cookie in Engine.WebDriver.Manage().Cookies.AllCookies)
            {
                var cookieToText =
                    string.Format("Cookie Data :{0}, Name: {1}{0} Value: {2}{0} Path: {3}{0} Domain: {4}{0}",
                        Environment.NewLine,
                        cookie.Name,
                        cookie.Value,
                        cookie.Path,
                        cookie.Domain);
                Log.LogInfo(cookieToText);
            }
        }

        /// <summary>
        ///     Check to see if the test is running on Jenkin's VMS
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningOnJenkins
        {
            get
            {
                var str = Environment.GetEnvironmentVariable("isjenkins");

                if (string.IsNullOrEmpty(str))
                {
                    Debug.WriteLine("Is Jenkins false (Environment variable 'ISJENKINS' is not set)");
                    return false;
                }

                bool isJenkins;
                if (bool.TryParse(str, out isJenkins))
                {
                    Debug.WriteLine("Is Jenkins {0}", isJenkins);
                    return isJenkins;
                }

                Debug.WriteLine(
                    "Is Jenkins false (Could not parse 'ISJENKINS' environment variable actual value '{0}')", str);
                return false;
            }
        }

        static SupportedBrowserType GetBrowserType
        {
            get
            {
                try
                {
                    var environmentData = string.Empty;
                    var fileData = string.Empty;
                    try
                    {
                        environmentData = Environment.GetEnvironmentVariable("BROWSERTYPE");
                        fileData = File.ReadAllText(@"C:\\BrowserType.txt");
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    if (!string.IsNullOrEmpty(fileData))
                    {
                        Log.LogInfo(fileData.Trim() + " Selected for browser");
                        if (fileData.ToLower().Contains("chrome"))
                            return SupportedBrowserType.Chrome;
                        if (fileData.ToLower().Contains("firefox"))
                            return SupportedBrowserType.Firefox;
                        if (fileData.ToLower().Contains("ie"))
                            return SupportedBrowserType.Ie;
                        if (fileData.ToLower().Contains("phantomjs"))
                            return SupportedBrowserType.PhantomJs;
                    }
                    if (!string.IsNullOrEmpty(environmentData))
                    {
                        Log.LogInfo(environmentData.Trim() + " Selected for browser");
                        if (environmentData.ToLower().Contains("chrome"))
                            return SupportedBrowserType.Chrome;
                        if (environmentData.ToLower().Contains("firefox"))
                            return SupportedBrowserType.Firefox;
                        if (environmentData.ToLower().Contains("ie"))
                            return SupportedBrowserType.Ie;
                        if (environmentData.ToLower().Contains("phantomjs"))
                            return SupportedBrowserType.PhantomJs;
                    }
                    return DefaultBrowserType;
                }
                catch (Exception)
                {
                    return DefaultBrowserType;
                }
            }
        }

        /// <summary>
        ///     Return the Default Window Size object of the Browser.
        /// </summary>
        public static void SetWindowSize()
        {
            try
            {
                var isJenkins = Environment.GetEnvironmentVariable("isjenkins");
                if (!string.IsNullOrEmpty(isJenkins) && isJenkins.ToLower() == "true")
                {
                    var size = new Size(1920, 2200);
                    Log.LogInfo("Setting the browser window size to {0}", size);
                    Engine.Execute<object>("window.resizeTo(1920, 2080);");
                }
                else
                {
                    Engine.WebDriver.Manage().Window.Maximize();
                }
                var resizedSize = Engine.WebDriver.Manage().Window.Size;
                Log.LogInfo("Browser window size is {0}x{1}", resizedSize.Width, resizedSize.Height);

            }
            catch (Exception ex)
            {
                var resizedSize = Engine.WebDriver.Manage().Window.Size;
                Log.LogError(
                    string.Format("ERROR: Browser window size is {0}x{1}", resizedSize.Width, resizedSize.Height),
                    exception: ex);
            }

        }

        /// <summary>
        ///     Check to see if the jQueryFrameWork is Loaded on a page.
        /// </summary>
        public static bool IsjQueryFrameWorkLoaded
        {
            get
            {
                var wait = new WebDriverWait(Engine.WebDriver, TimeSpan.FromMilliseconds(1000));
                try
                {
                    wait.Until(d =>
                    {
                        var isjQueryLoaded =
                            (bool)((RemoteWebDriver)d).ExecuteScript("return typeof $ !== 'undefined';");
                        return isjQueryLoaded;
                    });
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Couldn't load jQuery, probably there is an error in your test page: " + e.Message);
                    return false;
                }
                return false;
            }
        }

        /// <summary>
        ///     Count how many tabs/windows are open. (not iFrame related)
        /// </summary>
        public static int WindowTabCount
        {
            get
            {
                var availableWindows = Engine.WebDriver.WindowHandles;
                return availableWindows.Count;
            }
        }

        public static Engine SetupEngine()
        {
            return SetupEngine(DefaultBrowserType);
        }

        public static Engine ResetWebDriver()
        {
            var browserType = GetBrowserType;
            Engine.WebDriver.Close();
            return SetupEngine(browserType);
        }


        public static Engine SetupEngine(SupportedBrowserType browserType, string url = "", string driverType = "local")
        {
            var startEngine = new Engine();


            startEngine.Url = String.IsNullOrEmpty(url) ? "default" : url;
            BaseUrl = String.IsNullOrEmpty(url) ? "http://localhost.com/" : url;
            Debug.WriteLine(startEngine.Url);
            Engine.Browser = browserType;

            _driverType = String.IsNullOrEmpty(driverType) ? "Local" : driverType;

            if (_driverType == "")
            {
                throw new Exception(GlobalConfigurationFilePath +
                                    " is missing OR you can also please put 'DriverType' settings in Config File.");
            }

            #region driver_contruction

            Debug.WriteLine("Loading browser: " + Engine.Browser);
            if (_driverType.ToLower() == "local")
            {
                if (Engine.Browser == SupportedBrowserType.Ie) // Internet Explorer
                {
                    // clearIECache(); //optional
                    Engine.WebDriver = new EventFiringWebDriver(new InternetExplorerDriver(BrowserSetup.SetupInternetExplorerOptions));
                }
                else if (Engine.Browser == SupportedBrowserType.Chrome) //Google Chrome
                {
                    Engine.WebDriver = new EventFiringWebDriver(new ChromeDriver());
                }
                else if (Engine.Browser == SupportedBrowserType.Firefox) // Firefox
                {
                    Engine.WebDriver = new EventFiringWebDriver(new FirefoxDriver(BrowserSetup.FirefoxProfile));
                }
                else if (Engine.Browser == SupportedBrowserType.PhantomJs) // Phantom JS headless
                {
                    Engine.WebDriver = new EventFiringWebDriver(new PhantomJSDriver());
                }
                else
                {
                    // Default to Firefox (due to current state tests)
                    Engine.WebDriver = new EventFiringWebDriver(new FirefoxDriver(new FirefoxProfile()));
                }
            }
            else if (_driverType.ToLower() == "remote")
            {
                DesiredCapabilities browserCap = null;

                if (Engine.Browser == SupportedBrowserType.Ie)
                {
                    browserCap = BrowserSetup.SetupInternetExplorer;
                    var host = "http://127.0.0.1";
                    var port = 9515;
                    Engine.WebDriver = new EventFiringWebDriver(new RemoteWebDriver(new Uri(host + ":" + port), browserCap));
                }
                else if (Engine.Browser == SupportedBrowserType.Chrome)
                {
                    browserCap = BrowserSetup.SetupChrome;
                    var host = "http://127.0.0.1";
                    var port = 9515;
                    //var timeout = 5000;  // 5 seconds.

                    Engine.WebDriver = new EventFiringWebDriver(new RemoteWebDriver(new Uri(host + ":" + port), browserCap));
                    Engine.WebDriver.Navigate().GoToUrl(startEngine.Url);
                }
                else
                {
                    browserCap = BrowserSetup.SetupFirefox;
                    var host = "http://127.0.0.1";
                    var port = 9515;
                    Engine.WebDriver = new EventFiringWebDriver(new RemoteWebDriver(new Uri(host + ":" + port), browserCap));
                }
            }
            else
            {
                Debug.WriteLine("Error in identifying Local vs Remote");
            }

            if (!string.IsNullOrEmpty(startEngine.Url))
                Engine.WebDriver.Navigate().GoToUrl(startEngine.Url);

            #endregion

            //startEngine.FixtureSetup();  // Start Up WebDriver.


            return startEngine;
        }

        /// <summary>
        /// Clear the IE Cache before loading the browser.  There are different Codes that can perform different clears. 
        /// </summary>
        static void clearIECache()
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                FileName = "RunDll32.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                // Arguments = "InetCpl.cpl,ClearMyTracksByProcess 8"  //Delete Temporary Internet Files:
                Arguments = "InetCpl.cpl,ClearMyTracksByProcess 4351"  //Delete All + files and settings stored by Add-ons:
            };

            Process.Start(startInfo).WaitForExit(30000);
        }

        /// <summary>
        ///     Restart selenium WebDriver and use a different WebDriver Browser Type, like Chrome vs Firefox.
        /// </summary>
        /// <param name="browserType"></param>
        public void RestartAsBrowser(SupportedBrowserType browserType)
        {
            Engine.WebDriver.ExceptionThrown -= new SeleniumEventHandlers().firingDriver_ExceptionThrown;
            lock (_lock)
            {
                ShutDown();
                startAsBrowser(browserType);
            }
            Engine.WebDriver.ExceptionThrown += new SeleniumEventHandlers().firingDriver_ExceptionThrown;
        }

        /// <summary>
        ///     Run a pre-built javascript command
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public object RunCommand(string commandText, params object[] arguments)
        {
            var args = Array.ConvertAll(arguments, x => "\"" + x.ToString().Replace("\"", "'") + "\"");
            var script = string.Format("this.{0}({1})", commandText, String.Join(", ", args, 1));

            Debug.WriteLine("Running:  " + script);

            try
            {
                var returnValue = Execute<object>(script);
                Debug.WriteLine(returnValue);
                return returnValue;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed running Selenium script *{0}*\n{1}", script, e);
                Assert.Fail("Failed running Selenium script *{0}*\n{1}", script, e);
                return null;
            }
        }

        /// <summary>
        ///     Shutdown the entire Selenium WebDriver
        /// </summary>
        public static void ShutDown()
        {
            Engine.WebDriver.ExceptionThrown -= new SeleniumEventHandlers().firingDriver_ExceptionThrown;
            Engine.WebDriver.Navigating -= new SeleniumEventHandlers().firingDriver_Navigating;
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("ShutDown of Browser Took", timer)))
            {
                try
                {
                    lock (_lock)
                    {
                        Engine.WebDriver.Close();
                        Engine.WebDriver.Quit();
                    }
                }
                catch
                {
                    //ignore it.
                }
                finally
                {
                    // null out the webdriver.
                    Engine.WebDriver = null;
                }

            }
        }

        /// <summary>
        ///     Shutdown and reInitialize the Selenium WebDriver
        /// </summary>
        public void Reset()
        {
            ShutDown();
            InitializeBrowserInstance();
        }

        /// <summary>
        ///     De-serialize XML string to T object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns>T</returns>
        public static T FromXml<T>(String xml)
        {
            var returnedXmlClass = default(T);

            try
            {
                using (TextReader reader = new StringReader(xml))
                {
                    try
                    {
                        returnedXmlClass = (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                    }
                    catch (InvalidOperationException)
                    {
                        // String passed is not XML, simply return defaultXmlClass
                    }
                }
            }
            catch (Exception)
            {
            }

            return returnedXmlClass;
        }

        // [TestFixtureSetUp]
        public virtual void FixtureSetup()
        {
            //TODO add driver .start
        }

        /// <summary>
        ///     Open a URL in WebDriver
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public static string OpenUrl(string endpoint, string baseUrl = "")
        {
            return GoToUrl(endpoint, baseUrl);
        }

        /// <summary>
        ///     Open a URL in WebDriver
        /// </summary>
        /// <param name="endpoint">case</param>
        /// <param name="baseUrl">http://localhost/etc</param>
        /// <returns></returns>
        public static string GoToUrl(string endpoint, string baseUrl = "")
        {
            if (endpoint.StartsWith("http"))
            {
                return GoToUrl(new Uri(endpoint));
            }
            if (baseUrl.Length > 0)
            {
                return GoToUrl(new Uri(baseUrl + endpoint));
            }
            if (File.Exists(GlobalConfigurationFilePath))
            {
                var browserConfig = FromXml<BrowserConfig>(File.ReadAllText(GlobalConfigurationFilePath));
                return GoToUrl(new Uri(browserConfig.Url + endpoint));
            }

            // default BaseUrl from Engine
            return GoToUrl(new Uri(BaseUrl + endpoint));
        }

        /// <summary>
        ///     Open a URL in WebDriver, log in Stats D how long it took to load.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>Current URL after load</returns>
        public static string GoToUrl(Uri uri)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("GoToUrl", timer, WebDriver.Url)))
            {
                //   if (Engine.GetCurrentUrl.Contains("agent/login"))
                //      return Engine.GetCurrentUrl;
                Engine.WebDriver.Navigate().GoToUrl(uri);
                Support.WaitForPageReadyState(TimeSpan.FromSeconds(16));
            }
            return Engine.WebDriver.Url;
        }

        /// <summary>
        ///     Open a URL in a New Tab. (Same Window)
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="baseUrl"></param>
        public static void OpenUrlInNewTab(string endpoint, string baseUrl = "")
        {
            var body = FindElement(OpenQA.Selenium.By.TagName("body"));
            body.SendKeys(Keys.Control + 't');
            var uri = new Uri(baseUrl + endpoint);
            GoToUrl(uri);
        }

        /// <summary>
        ///     Takes the iframe and pulls it up into a new Tab. Useful for debugging issues.
        /// </summary>
        /// <param name="className">OPTIONAL example "stackFrame"</param>
        public static void OpeniFrameInNewTab(string className)
        {
            if (!string.IsNullOrEmpty(className))
            {
                Execute<object>("window.open(document.getElementsByClassName('" + className + "')[0].src,'_blank');");
            }
            else
            {
                Execute<object>("window.open(document.getElementsByTagName('iframe')[0].src,'_blank');");
            }
            try
            {
                var newTab = new List<string>(Engine.WebDriver.WindowHandles);
                Engine.WebDriver.SwitchTo().Window(newTab.Last());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Error in OpeniFrameInNewTab " + exception);
                throw;
            }
        }

        /// <summary>
        ///     Refreshes the current page via WebDriver native call
        /// </summary>
        public static void RefreshPage()
        {
            Log.LogDebug("# Reloading Page");
            Engine.WebDriver.Navigate().Refresh();
        }

        /// <summary>
        ///     Tries to refresh the browser window with Ctrl-F5
        /// </summary>
        public static void Refresh_Chrome()
        {
            var actionObject = new Actions(Engine.WebDriver);
            actionObject.KeyDown(Keys.Control).SendKeys(Keys.F5).KeyUp(Keys.Control).Perform();
        }

        /// <summary>
        ///     Pass this method the name of a cookie that is set or should be set in the browser at a certain
        /// </summary>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        public static bool VerifyCookieExists(string cookieName)
        {
            var cookiePresent = Engine.WebDriver.Manage().Cookies.GetCookieNamed(cookieName).ToString();
            return cookiePresent.Length > 0;
        }


        /// <summary> 
        ///  dc  Currently this is not turned back on.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void exceptionWatcher(object sender, FirstChanceExceptionEventArgs e)
        {
            if (
                e.Exception.Message.Contains(
                    "No connection could be made because the target machine actively refused it"))
            {
                //  throw new Exception("Chrome is failing");
                // No connection could be made because the target machine actively refused it [::1]:3431
                //     OutputTxt.WriteLine("Browser Could not Load. Make sure Chrome started with port 9515. No Connection Could be made to URL. Browser failed." + Environment.NewLine + e.Exception.Message);
                //    throw new Exception("Browser Could not Load. Make sure Chrome started with port 9515. No Connection Could be made to URL. Browser failed." + Environment.NewLine + e.Exception.Message);
            }
            else
            {
                if (e.Exception.GetType() == typeof(StaleElementReferenceException))
                {
                    //ignore
                }
                else
                {
                    switch (e.Exception.Source.ToLower())
                    {
                        case "Microsoft.CSharp":
                            if (isDuplicateError(e) == false)
                            {
                                Status = false;
                            }
                            break;
                        case "system":
                            if (isDuplicateError(e) == false)
                            {
                                //   Support.ScreenShot();
                                Status = false;
                            }
                            break;
                        case "selenium":
                            if (isDuplicateError(e) == false)
                            {
                                Debug.WriteLine(e.Exception.Message);
                                Support.ScreenShot();
                                Status = false;
                            }
                            break;
                        case "nunit.framework":
                            if (isDuplicateError(e) == false)
                            {
                                Debug.WriteLine(e.Exception.Message);
                                Support.ScreenShot();
                                Status = false;
                            }
                            break;
                        case "webdriver":
                            if (isDuplicateError(e) == false)
                            {
                                Debug.WriteLine(e.Exception.Message);
                                Support.ScreenShot();
                                Status = false;
                            }
                            break;
                        case "webdriver.support":
                            if (isDuplicateError(e) == false)
                            {
                                Debug.WriteLine(e.Exception.Message);
                                Support.ScreenShot();
                                Status = false;
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     This is used by the watcher method
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        static bool isDuplicateError(FirstChanceExceptionEventArgs e)
        {
            if (_lastMessage != e.Exception.Message)
            {
                _lastMessage = e.Exception.Message;
                return false;
            }
            return true;
        }

        public static void AlertGoAway()
        {
            try
            {
                // Check the presence of alert
                var alert = Engine.WebDriver.SwitchTo().Alert();
                Support.ScreenShot();
                // if present consume the alert
                alert.Accept();
            }
            catch (NoAlertPresentException)
            {
                // Alert not present
                //Debug.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        ///     Looks for Alerts in the WebDriver layer. This would be prompts to the user
        /// </summary>
        /// <returns></returns>
        public bool IsAlertPresent()
        {
            var presentFlag = false;

            try
            {
                // Check the presence of alert
                var alert = Engine.WebDriver.SwitchTo().Alert();
                // Alert present; set the flag
                presentFlag = true;
            }
            catch (NoAlertPresentException)
            {
                // Alert not present
                //Debug.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                // ignore
            }
            return presentFlag;
        }

        /// <summary>
        ///     Looks for Alerts in the WebDriver layer. This would be prompts to the user
        /// </summary>
        /// <returns></returns>
        public bool IsAlertNotPresent()
        {
            var presentFlag = false;

            try
            {
                // Check the presence of alert
                var alert = Engine.WebDriver.SwitchTo().Alert();
                // Alert present; set the flag
                presentFlag = true;
            }
            catch (NoAlertPresentException)
            {
                // Alert not present
                //Debug.WriteLine(ex.Message);
            }
            return !presentFlag;
        }

        static void startAsBrowser(SupportedBrowserType browserType)
        {
            lock (_lock)
            {
                Log.LogInfo("Initializing {0} web driver browser instance", browserType);


                var engine = SetupEngine(browserType, ConfigurationManager.AppSettings["InitialPageUrl"], "local");

                if (engine == null || Engine.WebDriver == null)
                {
                    Log.LogInfo("Engine has not started.");
                    throw new Exception("Error setting engine window size.  Engine was not detected.");

                }

                SetWindowSize();

                try
                {
                    Engine.WebDriver
                        .Manage()
                        .Timeouts()
                        .ImplicitlyWait(TimeSpan.FromSeconds(2))
                        .SetScriptTimeout(TimeSpan.FromSeconds(10));
                }
                catch (Exception e)
                {
                    Log.LogError("Problem configuring RemoteWebDriver. ", e);
                    Assert.Fail("Problem configuring RemoteWebDriver.");
                }
            }
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForExistance(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            try
            {
                Log.LogDebug(string.Format("Waiting for {0} to be present.", by));
                var element = Support.WaitUntilElementIsPresent(by, waitTimeSpan);
                return element;
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
            return null;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForExistance(By by, TimeSpan waitTimeSpan)
        {
            try
            {
                Log.LogDebug(string.Format("Waiting for {0} to exist / be present.", by));
                var element = Support.WaitUntilElementIsPresent(by, waitTimeSpan);
                return element;
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
            return null;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForExistance(OpenQA.Selenium.By by)
        {
            try
            {
                Log.LogDebug(string.Format("Waiting for {0} to exist / be present.", by));
                var element = Support.WaitUntilElementIsPresent(by);
                return element;
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
            return null;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForExistance(By by)
        {
            try
            {
                Log.LogDebug(string.Format("Waiting for {0} to exist / be present.", by));
                var element = Support.WaitUntilElementIsPresent(by);
                return element;
            }
            catch (Exception ex)
            {
                Log.LogException(ex);
            }
            return null;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForVisible(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            var element = Support.WaitUntilElementIsVisible(by, waitTimeSpan);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForVisible(By by, TimeSpan waitTimeSpan)
        {
            var element = Support.WaitUntilElementIsVisible(by, waitTimeSpan);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForVisible(OpenQA.Selenium.By by)
        {
            var element = Support.WaitUntilElementIsVisible(by);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static IWebElement WaitForVisible(By by)
        {
            var element = Support.WaitUntilElementIsVisible(by);
            return element;
        }


        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static bool WaitForNotVisible(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            var element = Support.WaitUntilElementIsNotVisible(by, waitTimeSpan);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static bool WaitForNotVisible(By by, TimeSpan waitTimeSpan)
        {
            var element = Support.WaitUntilElementIsNotVisible(by, waitTimeSpan);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static bool WaitForNotVisible(OpenQA.Selenium.By by)
        {
            var element = Support.WaitUntilElementIsNotVisible(by);
            return element;
        }

        /// <summary>
        /// This extends the IWebElement and adds the ability for a wait to occur
        /// </summary>
        public static bool WaitForNotVisible(By by)
        {
            var element = Support.WaitUntilElementIsNotVisible(by);
            return element;
        }



        /// <summary>
        ///     Execute javascript: return jQuery('iframe.ui-helper-hidden-accessible').get(0)
        /// </summary>
        public void WaitTilLoaded()
        {
            var wait = new DefaultWait<IJavaScriptExecutor>(Engine.WebDriver)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            wait.Until(
                d =>
                    (d.ExecuteScript("return jQuery('iframe.ui-helper-hidden-accessible').get(0)") as IWebElement) ==
                    null);
        }

        /// <summary>
        ///     If jQuery is active, then a javascript call is in progress.
        /// </summary>
        public static void WaitForAjaxToComplete()
        {
            while (true) // Handle timeout somewhere
            {
                var javaScriptExecutor = Engine.WebDriver as IJavaScriptExecutor;
                var ajaxIsComplete = javaScriptExecutor != null
                                     && (bool)javaScriptExecutor.ExecuteScript("return jQuery.active == 0");
                if (ajaxIsComplete)
                    break;
                Thread.Sleep(200);
            }
        }

        #region Javascript Execution

        /// <summary>
        ///     Execute javascript , returns a string
        ///     remember that any javascript written to page will only exist temp.  If you wish to add function, then use it. Write
        ///     that in one call.
        /// </summary>
        /// <param name="javaScript"></param>
        /// <returns>string</returns>
        public static string ExecuteJavaScript(string javaScript)
        {
            try
            {
                return (string)((IJavaScriptExecutor)Engine.WebDriver).ExecuteScript(javaScript);
            }
            catch (UnhandledAlertException unhandledAlertException)
            {
                throw new Exception(unhandledAlertException.Message + " " + unhandledAlertException.AlertText);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     usage
        ///     remember that any javascript written to page will only exist temp.  If you wish to add function, then use it. Write
        ///     that in one call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script"></param>
        /// <returns>T</returns>
        public static T Execute<T>(string script)
        {
            try
            {
                return (T)((IJavaScriptExecutor)Engine.WebDriver).ExecuteScript(script);
            }
            catch (UnhandledAlertException unhandledAlertException)
            {
                throw new Exception(unhandledAlertException.Message + " " + unhandledAlertException.AlertText);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        ///     usage
        ///     remember that any javascript written to page will only exist temp.  If you wish to add function, then use it. Write
        ///     that in one call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="driver">Engine.WebDriver</param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static T Execute<T>(IWebDriver driver, string script)
        {
            try
            {
                return (T)((IJavaScriptExecutor)driver).ExecuteScript(script);
            }
            catch (UnhandledAlertException unhandledAlertException)
            {
                throw new Exception(unhandledAlertException.Message + " " + unhandledAlertException.AlertText);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        ///     usage
        ///     remember that any javascript written to page will only exist temp.  If you wish to add function, then use it. Write
        ///     that in one call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="script"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Execute<T>(string script, params object[] args)
        {
            try
            {
                return (T)((IJavaScriptExecutor)Engine.WebDriver).ExecuteScript(script, args);
            }
            catch (UnhandledAlertException unhandledAlertException)
            {
                throw new Exception(unhandledAlertException.Message + " " + unhandledAlertException.AlertText);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        ///     usage
        ///     remember that any javascript written to page will only exist temp.  If you wish to add function, then use it. Write
        ///     that in one call.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="driver"></param>
        /// <param name="script"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Execute<T>(IWebDriver driver, string script, params object[] args)
        {
            try
            {
                return (T)((IJavaScriptExecutor)driver).ExecuteScript(script, args);
            }
            catch (UnhandledAlertException unhandledAlertException)
            {
                throw new Exception(unhandledAlertException.Message + " " + unhandledAlertException.AlertText);
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region Find Controls

        public static Control FindControl(OpenQA.Selenium.By by)
        {
            try
            {
                return new Control(Engine.WebDriver.FindElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindControl(By by)
        {
            try
            {
                return new Control(Engine.WebDriver.FindElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindControl(CustomBy by)
        {
            try
            {
                return new Control(Engine.WebDriver.FindElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindControlWithWait(By by, TimeSpan? waitTimeSpan = null, bool safeMode = true)
        {
            if (waitTimeSpan == null)
            {
                waitTimeSpan = TimeSpan.FromSeconds(5);
            }
            try
            {
                new WebDriverWait(Engine.WebDriver, (TimeSpan)waitTimeSpan)
                    .Until(ExpectedConditions.ElementExists(by));

                return new Control(Engine.WebDriver.FindElement(by));
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        public static Control FindChildControl(IWebElement parentElement, OpenQA.Selenium.By by)
        {
            try
            {
                return new Control(parentElement.FindElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindChildControl(Control parentControl, OpenQA.Selenium.By by)
        {
            try
            {
                return new Control(parentControl.GetChildElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindChildControl(Control parentControl, By by)
        {
            try
            {
                return new Control(parentControl.GetChildElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static Control FindChildControl(Control parentControl, CustomBy by)
        {
            try
            {
                return new Control(parentControl.GetChildElement(@by));
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        #endregion

        #region  Find Elements

        public static IWebElement FindChildElement(IWebElement parentElement, OpenQA.Selenium.By by, bool safeMode = true)
        {
            try
            {
                return parentElement.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        public static IWebElement FindChildElement(IWebElement parentElement, By by, bool safeMode = true)
        {
            try
            {
                return parentElement.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        public static IWebElement FindChildElement(IWebElement parentElement, CustomBy by, bool safeMode = true)
        {
            try
            {
                return parentElement.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        public static ReadOnlyCollection<IWebElement> FindChildElements(IWebElement parentElement, OpenQA.Selenium.By by)
        {
            try
            {
                var returnSet = parentElement.FindElements(@by);
                return returnSet;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static ReadOnlyCollection<IWebElement> FindChildElements(IWebElement parentElement, By by)
        {
            try
            {
                return parentElement.FindElements(@by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static ReadOnlyCollection<IWebElement> FindChildElements(IWebElement parentElement, CustomBy by)
        {
            try
            {
                return parentElement.FindElements(@by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        public static IWebElement FindElement(PageObject.How how, string selector, string value = null)
        {
            try
            {
                OpenQA.Selenium.By myBy;
                switch (how)
                {
                    case PageObject.How.ClassName:
                        myBy = OpenQA.Selenium.By.ClassName(selector);
                        break;
                    case PageObject.How.CssSelector:
                        myBy = OpenQA.Selenium.By.CssSelector(selector);
                        break;
                    case PageObject.How.Id:
                        myBy = OpenQA.Selenium.By.Id(selector);
                        break;
                    case PageObject.How.LinkText:
                        myBy = OpenQA.Selenium.By.LinkText(selector);
                        break;
                    case PageObject.How.Name:
                        myBy = OpenQA.Selenium.By.Name(selector);
                        break;
                    case PageObject.How.PartialLinkText:
                        myBy = OpenQA.Selenium.By.PartialLinkText(selector);
                        break;
                    case PageObject.How.TagName:
                        myBy = OpenQA.Selenium.By.TagName(selector);
                        break;
                    case PageObject.How.XPath:
                        myBy = OpenQA.Selenium.By.XPath(selector);
                        break;
                    case PageObject.How.jQuery:
                        myBy = CustomBy.jQuery(selector);
                        break;
                    case PageObject.How.HtmlTag:
                        myBy = CustomBy.HtmlTag(selector, value);
                        break;
                    case PageObject.How.DataSupapicka:
                        myBy = CustomBy.DataSupapicka(selector);
                        break;
                    case PageObject.How.CssValue:
                        myBy = CustomBy.CssValue(selector);
                        break;
                    case PageObject.How.Src:
                        myBy = CustomBy.Src(selector);
                        break;
                    default:
                        myBy = OpenQA.Selenium.By.Name(selector);
                        break;
                }
                return WebDriver.FindElement(myBy);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static IWebElement FindElement(OpenQA.Selenium.By by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError("FindElement issue: " + ex);
                //ignore
                return null;
            }
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static IWebElement FindElement(By by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError("FindElement issue: " + ex);
                //ignore
                return null;
            }
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static IWebElement FindElement(CustomBy by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError("FindElement issue: " + ex);
                //ignore
                return null;
            }
        }

        public static IWebElement FindElementByJs(IWebDriver driver, string jsCommand)
        {
            return (IWebElement)((IJavaScriptExecutor)driver).ExecuteScript(jsCommand);
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by">With this you can use multiple By Objects</param>
        /// <returns></returns>
        public static IWebElement FindElement(params OpenQA.Selenium.By[] by)
        {
            var ex = "";

            foreach (var eachBy in @by)
            {
                try
                {
                    return FindElement(eachBy);
                }
                catch (NoSuchElementException)
                {
                    ex += "Unable to find element with " + eachBy + Environment.NewLine;
                }
            }

            throw new NoSuchElementException(ex);
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by">With this you can use multiple By Objects</param>
        /// <returns></returns>
        public static IWebElement FindElement(params By[] by)
        {
            var ex = "";

            foreach (var eachBy in @by)
            {
                try
                {
                    return FindElement(eachBy);
                }
                catch (NoSuchElementException)
                {
                    ex += "Unable to find element with " + eachBy + Environment.NewLine;
                }
            }

            throw new NoSuchElementException(ex);
        }

        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by">With this you can use multiple By Objects</param>
        /// <returns></returns>
        public static IWebElement FindElement(params CustomBy[] by)
        {
            var ex = "";

            foreach (var eachBy in @by)
            {
                try
                {
                    return FindElement(eachBy);
                }
                catch (NoSuchElementException)
                {
                    ex += "Unable to find element with " + eachBy + Environment.NewLine;
                }
            }

            throw new NoSuchElementException(ex);
        }


        /// <summary>
        ///     Find element "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="waitTimeSpan"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static IWebElement FindElementWithWait(OpenQA.Selenium.By by, TimeSpan? waitTimeSpan = null,
            bool safeMode = true)
        {
            if (waitTimeSpan == null)
            {
                waitTimeSpan = TimeSpan.FromSeconds(5);
            }
            try
            {
                new WebDriverWait(Engine.WebDriver, (TimeSpan)waitTimeSpan)
                    .Until(ExpectedConditions.ElementExists(by));

                return WebDriver.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;

                throw;
            }
        }

        public static ReadOnlyCollection<IWebElement> FindElements(PageObject.How how, string selector,
            string value = null)
        {
            try
            {
                OpenQA.Selenium.By myBy;
                switch (how)
                {
                    case PageObject.How.ClassName:
                        myBy = OpenQA.Selenium.By.ClassName(selector);
                        break;
                    case PageObject.How.CssSelector:
                        myBy = OpenQA.Selenium.By.CssSelector(selector);
                        break;
                    case PageObject.How.Id:
                        myBy = OpenQA.Selenium.By.Id(selector);
                        break;
                    case PageObject.How.LinkText:
                        myBy = OpenQA.Selenium.By.LinkText(selector);
                        break;
                    case PageObject.How.Name:
                        myBy = OpenQA.Selenium.By.Name(selector);
                        break;
                    case PageObject.How.PartialLinkText:
                        myBy = OpenQA.Selenium.By.PartialLinkText(selector);
                        break;
                    case PageObject.How.TagName:
                        myBy = OpenQA.Selenium.By.TagName(selector);
                        break;
                    case PageObject.How.XPath:
                        myBy = OpenQA.Selenium.By.XPath(selector);
                        break;
                    case PageObject.How.jQuery:
                        myBy = CustomBy.jQuery(selector);
                        break;
                    case PageObject.How.HtmlTag:
                        myBy = CustomBy.HtmlTag(selector, value);
                        break;
                    case PageObject.How.Src:
                        myBy = CustomBy.Src(selector);
                        break;
                    case PageObject.How.DataSupapicka:
                        myBy = CustomBy.DataSupapicka(selector);
                        break;
                    case PageObject.How.CssValue:
                        myBy = CustomBy.CssValue(selector);
                        break;
                    default:
                        myBy = OpenQA.Selenium.By.Name(selector);
                        break;
                }
                return WebDriver.FindElements(myBy);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Find elements "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<IWebElement> FindElements(OpenQA.Selenium.By by)
        {
            try
            {
                return Engine.WebDriver.FindElements(@by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Find elements "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            try
            {
                return Engine.WebDriver.FindElements(@by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Find elements "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        public static ReadOnlyCollection<IWebElement> FindElements(CustomBy by)
        {
            try
            {
                return Engine.WebDriver.FindElements(@by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }

        #endregion

        #region Is Element Present / Displayed

        /// <summary>
        ///     Looks for Element.Displayed "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static bool IsElementVisible(IWebElement element, bool safeMode = true)
        {
            try
            {
                return element.Displayed;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return false;
                throw;
            }
            catch (Exception)
            {
                if (safeMode)
                    return false;
                throw;
            }
        }

        /// <summary>
        ///     Looks for Element.Displayed "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static bool IsElementVisible(OpenQA.Selenium.By by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by).Displayed;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return false;
                throw;
            }
            catch (Exception)
            {
                if (safeMode)
                    return false;
                throw;
            }
        }

        /// <summary>
        ///     Looks for Element.Displayed "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static bool IsElementVisible(By by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by).Displayed;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return false;
                throw;
            }
            catch (Exception)
            {
                if (safeMode)
                    return false;
                throw;
            }
        }

        /// <summary>
        ///     Looks for Element.Displayed "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static bool IsElementPresent(IWebElement element, bool safeMode = true)
        {
            try
            {
                var tag = element.TagName;
                return tag != null;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return false;
                throw;
            }
            catch (Exception)
            {
                if (safeMode)
                    return false;
                throw;
            }
        }

        /// <summary>
        ///     Engine.IsElementPresent(By.Id("userName"), By.Name("userName")).ToString();
        /// </summary>
        /// <param name="by">With this you can use multiple By objects to find element</param>
        /// <returns></returns>
        public static bool IsElementPresent(By by)
        {
            try
            {
                var element = FindElement(@by, false);
                if (element != null)
                    return true;
                throw new NoSuchElementException();
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Engine.IsElementPresent(By.Id("userName"), By.Name("userName")).ToString();
        /// </summary>
        /// <param name="by">With this you can use multiple By objects to find element</param>
        /// <returns></returns>
        public static bool IsElementPresent(OpenQA.Selenium.By by)
        {
            try
            {
                var element = FindElement(@by, false);
                if (element != null)
                    return true;
                throw new NoSuchElementException();
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Engine.IsElementPresent(By.Id("userName"), By.Name("userName")).ToString();
        /// </summary>
        /// <param name="by">With this you can use multiple By objects to find element</param>
        /// <returns></returns>
        public static bool IsElementPresent(params By[] by)
        {
            try
            {
                var element = FindElement(@by);
                if (element != null)
                    return true;
                throw new NoSuchElementException();
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }


        /// <summary>
        ///     Looks for Element.Displayed "Safely" if it is not on the page, return null.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public static bool IsElementVisible(CustomBy by, bool safeMode = true)
        {
            try
            {
                return Engine.WebDriver.FindElement(@by).Displayed;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return false;
                throw;
            }
            catch (Exception)
            {
                if (safeMode)
                    return false;
                throw;
            }
        }

        /// <summary>
        ///     Engine.IsElementVisible(By.Id("userName"), By.Name("userName")).ToString();
        /// </summary>
        /// <param name="by">With this you can use multiple By objects to find element</param>
        /// <returns></returns>
        public static bool IsElementVisible(params By[] by)
        {
            try
            {
                var element = FindElement(@by);
                if (element != null)
                    return element.Displayed;
                throw new NoSuchElementException();
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        #endregion

        #region Get Methods on WebDriver page methods

        /// <summary>
        ///     Gets a cookie with the specified name.
        /// </summary>
        /// <param name="cookieKey"></param>
        /// <returns></returns>
        public static string GetCookieValue(string cookieKey)
        {
            string value = null;
            var cookie = Engine.WebDriver.Manage().Cookies.GetCookieNamed(cookieKey);

            if (cookie != null)
            {
                value = cookie.Value;
            }

            return value;
        }

        /// <summary>
        ///     Gets the URL the browser is currently displaying
        /// </summary>
        public static string GetCurrentUrl
        {
            get
            {
                try
                {
                    Support.WaitForPageReadyState();
                    return WebDriver.Url;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Gets the source of the page from last WebDriver page call
        /// </summary>
        public static string GetEntirePageSource
        {
            get { return Engine.WebDriver.PageSource; }
        }

        /// <summary>
        ///     Get Page Text (not source) of the current page
        /// </summary>
        public static string GetPageText
        {
            get { return Engine.WebDriver.FindElement(OpenQA.Selenium.By.TagName("body")).Text; }
        }

        /// <summary>
        ///     Check to see if there is an iFrame tag on the current page.
        /// </summary>
        public static bool HasiFrame
        {
            get
            {
                var frameElements = Engine.WebDriver.FindElements(OpenQA.Selenium.By.TagName("iframe"));
                if (frameElements.Count <= 0) return false;
                return frameElements.Any(frame => frame.Displayed);
            }
        }

        /// <summary>
        ///     Check to see if there is an iFrame tag on the current page.
        /// </summary>
        public static int IframeCount
        {
            get
            {
                var frameElements = Engine.WebDriver.FindElements(OpenQA.Selenium.By.TagName("iframe"));
                if (frameElements.Count <= 0) return 0;
                return frameElements.Count(frame => frame.Displayed);
            }
        }

        /// <summary>
        ///     Check to see if there is an iFrame tag on the current page.
        /// </summary>
        public static ReadOnlyCollection<IWebElement> IframesCollection
        {
            get
            {
                var frameElements = Engine.WebDriver.FindElements(OpenQA.Selenium.By.TagName("iframe"));
                if (frameElements.Count <= 0) return null;
                return frameElements;
            }
        }

        /// <summary>
        ///     Is there an 403 error on page
        /// </summary>
        public static bool Is403Error
        {
            get
            {
                try
                {
                    if (!Engine.WebDriver.PageSource.Contains("HTTP Error 403"))
                    {
                        return false;
                    }
                    Log.LogDebug("403 Forbidden Page");
                    Support.ScreenShot();
                    return true;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        /// <summary>
        ///     Is there an 404 error on page
        /// </summary>
        public static bool Is404Error
        {
            get
            {
                try
                {
                    if (!Engine.WebDriver.PageSource.Contains("HTTP Error 404"))
                    {
                        return false;
                    }
                    Log.LogDebug("404 error found in page");
                    Support.ScreenShot();
                    return true;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        /// <summary>
        ///     is there a 500 error
        /// </summary>
        public static bool Is500Error
        {
            get
            {
                if (!Engine.WebDriver.PageSource.Contains("500 Error") && !Engine.WebDriver.PageSource.Contains("Server Error in"))
                {
                    return false;
                }
                Engine.WebDriver.Navigate().Refresh();
                Thread.Sleep(2000);
                // try 1 more time to keep tests running properly on low CPU/ memory
                if (!Engine.WebDriver.PageSource.Contains("500 Error") && !Engine.WebDriver.PageSource.Contains("Server Error in"))
                {
                    return false;
                }
                Log.LogDebug("500 error found in page");
                Support.ScreenShot();
                return true;
            }
        }

        #endregion

        #region iFrame methods

        /// <summary>
        ///     Select a frame by an element in it. (Searches all iframes)
        /// </summary>
        public static IWebElement SwitchToiFrameToFindElement(OpenQA.Selenium.By by)
        {
            if (IframeCount > 0)
            {
                foreach (var frame in FindElements(OpenQA.Selenium.By.TagName("iframe")))
                {
                    Engine.WebDriver.SwitchTo().Frame(frame);
                    if (FindElement(by) != null)
                    {
                        return frame;
                        // stay on frame and exit
                    }
                }
                Engine.WebDriver.SwitchTo().DefaultContent();
                if (FindElement(by) != null)
                {
                    throw new NoSuchElementException("No element found on any frame.");
                }
            }
            return null;
        }


        /// <summary>
        ///     Select a frame by it's Zero-Based ID
        /// </summary>
        /// <param name="frameIndex"></param>
        public static IWebDriver SwitchToiFrame(int? frameIndex = 0)
        {
            if (frameIndex == null)
                frameIndex = 0;
            return Engine.WebDriver.SwitchTo().Frame((int)frameIndex);
        }

        /// <summary>
        ///     Select an iFrame by name or ID.
        /// </summary>
        /// <param name="nameOrid"></param>
        public static IWebDriver SwitchToiFrame(string nameOrid)
        {
            return Engine.WebDriver.SwitchTo().Frame(nameOrid);
        }

        /// <summary>
        ///     Select an iFrame by @@attribute.
        /// </summary>
        /// <param name="attribute">Do not put @ in name</param>
        /// <param name="value">/agent/choose/Employee?CorrelationId=</param>
        public static IWebDriver SwitchToiFrameWithAttribute(string @attribute, string value)
        {
            Engine.WebDriver.SwitchTo().DefaultContent();
            Support.WaitForPageReadyState();
            var frameElement = FindElement(
                OpenQA.Selenium.By.XPath(
                    string.Format("//iframe[contains(@{0},'{1}')]", @attribute, value)));
            if (frameElement == null)
                return null;
            var newFrame = Engine.WebDriver.SwitchTo().Frame(frameElement);
            Support.WaitForPageReadyState();
            return newFrame;
        }

        /// <summary>
        ///     Select an iFrame by src attribute of an iframe (partial text match)
        /// </summary>
        /// <param name="src">src attribute of an iframe (partial text match)</param>
        public static IWebDriver SwitchToiFrameWithSrc(string src)
        {
            var iFrame = FindElement(PageObject.How.Src, src);
            if (iFrame == null)
            {
                Engine.SwitchOutofiFrames();
                iFrame = FindElement(PageObject.How.Src, src);
            }
            if (iFrame == null)
                throw new Exception("no iframe found for " + src);
            _log.LogDebug("Switching to iframe: " + src);
            return Engine.WebDriver.SwitchTo().Frame(iFrame);
        }

        /// <summary>
        ///     Select an iFrame by @class.
        /// </summary>
        /// <param name="class"></param>
        public static IWebDriver SwitchToiFrameWithClass(string @class)
        {
            var iFrame = FindElement(OpenQA.Selenium.By.ClassName(@class));
            if (iFrame == null)
            {
                Engine.SwitchOutofiFrames();
                iFrame = FindElement(OpenQA.Selenium.By.ClassName(@class));
            }
            if (iFrame == null)
                throw new Exception("no iframe found for " + @class);
            _log.LogDebug("Switching to iframe: " + @class);
            return Engine.WebDriver.SwitchTo().Frame(iFrame);

        }

        /// <summary>
        ///     Select a frame by it's previously found WebElement
        ///     example: Engine.SwitchToiFrame(Engine.FindElement(By.cssSelector("iframe[title='Fill Quote']")));
        ///     example: Engine.SwitchToiFrame(Engine.FindElement(By.tagName("iframe")).get(1));
        /// </summary>
        /// <param name="frameElement">Must be a frame IwebElement</param>
        public static IWebDriver SwitchToiFrame(IWebElement frameElement)
        {
            return Engine.WebDriver.SwitchTo().Frame(frameElement);
        }

        /// <summary>
        ///     Selects either the first frame on the page or the main document when a page contains iFrames.
        ///     An OpenQA.Selenium.IWebDriver instance focused on the default frame.
        ///     Clone of: SwitchOutToDefaultContent for Ease of use in Finding a method.
        /// </summary>
        public static void SwitchOutofiFrames()
        {
            _log.LogInfo("Switching out of iFrames");
            WebDriver.SwitchTo().Window(WebDriver.WindowHandles.First());
            // WebDriver.SwitchTo().DefaultContent();
        }

        /// <summary>
        ///     Selects either the first frame on the page or the main document when a page contains iFrames.
        ///     An OpenQA.Selenium.IWebDriver instance focused on the default frame.
        /// </summary>
        public static void SwitchOutToDefaultContent()
        {
            _log.LogInfo("Switching out to DefaultContext");
            WebDriver.SwitchTo().DefaultContent();
        }

        /// <summary>
        ///     Switches the focus of future commands for this driver to the window with the given name.
        /// </summary>
        /// <param name="title">title: The title or part of string of the window to select.</param>
        public static bool SwitchWindow(string title)
        {
            var currentWindow = Engine.WebDriver.CurrentWindowHandle;
            var availableWindows = new List<string>(WebDriver.WindowHandles);

            foreach (var w in availableWindows)
            {
                if (w != currentWindow)
                {
                    Engine.WebDriver.SwitchTo().Window(w);
                    if (Engine.WebDriver.Title.Contains(title))
                        return true;
                    Engine.WebDriver.SwitchTo().Window(currentWindow);
                }
            }
            return false;
        }

        /// <summary>
        ///     Switch to the last Tab/Window Opened (not iFrame)
        /// </summary>
        public static void SwitchToWindowLastOpened()
        {
            Engine.WebDriver.SwitchTo().Window(WebDriver.WindowHandles.Last());
        }

        /// <summary>
        ///     Switch to the Tab/Window with the part of the Url inputed
        /// </summary>
        /// <param name="urlPrefix"></param>
        public static void SwitchToWindowWithUrl(string urlPrefix)
        {
            var handlers = Engine.WebDriver.WindowHandles;

            foreach (var handler in handlers)
            {
                Engine.WebDriver.SwitchTo().Window(handler);
                if (Engine.WebDriver.Url.Contains(urlPrefix)) return;
            }
        }

        /// <summary>
        ///     Provides a mechanism by which the window handle of an invoked popup browser window may be determined.
        /// </summary>
        /// <param name="by"></param>
        public static void SwitchToPopupWindow(OpenQA.Selenium.By by)
        {
            //var current = WebDriver.CurrentWindowHandle;
            var finder = new PopupWindowFinder(WebDriver);
            var newHandle = finder.Click(WebDriver.FindElement(by));
            WebDriver.SwitchTo().Window(newHandle);
        }

        /// <summary>
        ///     Provides a mechanism by which the window handle of an invoked popup browser window may be determined.
        /// </summary>
        /// <param name="by"></param>
        public static void SwitchToPopupWindow(By by)
        {
            //var current = WebDriver.CurrentWindowHandle;
            var finder = new PopupWindowFinder(Engine.WebDriver);
            var newHandle = finder.Click(Engine.WebDriver.FindElement(by));
            Engine.WebDriver.SwitchTo().Window(newHandle);
        }

        /// <summary>
        ///     Provides a mechanism by which the window handle of an invoked popup browser window may be determined.
        /// </summary>
        /// <param name="by"></param>
        public static void SwitchToPopupWindow(CustomBy by)
        {
            //var current = WebDriver.CurrentWindowHandle;
            var finder = new PopupWindowFinder(Engine.WebDriver);
            var newHandle = finder.Click(Engine.WebDriver.FindElement(by));
            Engine.WebDriver.SwitchTo().Window(newHandle);
        }

        #endregion

        #region Scroll Methods

        internal enum ScrollDirection
        {
            Up,
            Down
        }

        /// <summary>
        ///     uses the Dovetail slimScrollBar, to look for FA elements
        /// </summary>
        /// <param name="how"></param>
        /// <param name="childSelector"></param>
        public static void SlimScrollerScrollElementIntoView(PageObject.How how, string childSelector)
        {
            try
            {
                var childElement = FindElement(how, childSelector);
                if (childElement.Displayed)
                {
                    return;
                }

                ScrollToTop();

                var lastIndex = CurrentScrollIndex;
                while (!childElement.Displayed && scroll(ScrollDirection.Down) != lastIndex)
                {
                    lastIndex = CurrentScrollIndex;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ScrollElementIntoView: ignoring this error " + ex.Message);
                throw;
            }
        }

        public static int CurrentScrollIndex
        {
            get
            {
                var scrollBar = FindElement(PageObject.How.ClassName, "slimScrollBar");

                var top = scrollBar.GetCssValue("top").Trim(' ', 'p', 'x', ';');

                int topIndex;
                return Int32.TryParse(top, out topIndex) ? topIndex : -1;
            }
        }

        /// <summary>
        ///     uses the Dovetail slimScrollBar, scrolls down.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        static int scroll(ScrollDirection direction)
        {
            var actions = new Actions(WebDriver);

            var scrollBar = FindElement(PageObject.How.ClassName, "slimScrollBar");
            if (!scrollBar.Displayed) return 0;

            actions = new Actions(WebDriver);
            var offset = ScrollDirection.Up == direction ? -10 : 10;

            actions.MoveToElement(scrollBar).DragAndDropToOffset(scrollBar, 0, offset).Build().Perform();
            return CurrentScrollIndex;
        }

        public static void ScrollToTop()
        {
            try
            {
                while (scroll(ScrollDirection.Up) > 0)
                {
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("SlimScrollDriver: Cannot scroll to top, scroll bars may not be needed.");
                //ignore error
            }
        }

        public static void ScrollToBottom()
        {
            try
            {
                var lastIndex = CurrentScrollIndex;
                while (scroll(ScrollDirection.Down) != lastIndex)
                {
                    lastIndex = CurrentScrollIndex;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("SlimScrollDriver: Cannot scroll to bottom, scroll bars may not be needed.");
                //ignore error
            }
        }

        public static void ScrollToTopOfPage()
        {
            try
            {
                IWebElement scroll = FindElement(By.TagName("body"));
                scroll.SendKeys(Keys.PageUp);
                scroll.SendKeys(Keys.PageUp);
            }
            catch (Exception)
            {
                // ignore 
            }
        }

        public static void ScrollToBottomOfPage()
        {
            try
            {
                IWebElement scroll = FindElement(By.TagName("body"));
                scroll.SendKeys(Keys.PageDown);
                scroll.SendKeys(Keys.PageDown);
            }
            catch (Exception)
            {
                // ignore 
            }
        }

        #endregion
    }


    public static class Extension
    {
        public static T ToEnum<T>(this string enumString)
        {
            return (T)Enum.Parse(typeof(T), enumString);
        }
    }

    public enum SupportedBrowserType
    {
        Firefox,
        Ie,
        Chrome,
        PhantomJs
    }

    public interface IBrowserDriver : IDisposable
    {
        SupportedBrowserType BrowserType { get; }
        IWebElement FindElement(OpenQA.Selenium.By selector);
        IEnumerable<IWebElement> FindElements(OpenQA.Selenium.By selector);
        void ShutDown();
        void Reset();
    }

    public class BrowserSetup
    {
        // static readonly string GoogleChromeVersion = ConfigurationManager.AppSettings["GoogleChromeVersion"];
        // static readonly string FireFoxVersion = ConfigurationManager.AppSettings["FireFoxVersion"];

        // static readonly string IEVersion = ConfigurationManager.AppSettings["IEVersion"];

        public static DesiredCapabilities SetupFirefox
        {
            get
            {
                var capability = DesiredCapabilities.Firefox();
                //capability.SetCapability(CapabilityType.Version, FireFoxVersion);
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static FirefoxProfile FirefoxProfile
        {
            get
            {
                var ffProfile = new FirefoxProfile();
                ffProfile.SetPreference("webdriver.firefox.useExisting", true);
                ffProfile.SetPreference("browser.cache.disk.enable", false);
                ffProfile.SetPreference("browser.cache.memory.enable", false);
                ffProfile.SetPreference("webdriver.load.strategy", "unstable");
                return ffProfile;
            }
        }

        public static DesiredCapabilities SetupFirefox36
        {
            get
            {
                var capability = DesiredCapabilities.Firefox();
                capability.SetCapability(CapabilityType.Version, "3.6");
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static InternetExplorerOptions SetupInternetExplorerOptions
        {
            get
            {
                var options = new InternetExplorerOptions
                {
                    IntroduceInstabilityByIgnoringProtectedModeSettings = true,
                    IgnoreZoomLevel = true,
                    UnexpectedAlertBehavior = InternetExplorerUnexpectedAlertBehavior.Accept,
                    RequireWindowFocus = false,
                    EnablePersistentHover = true,
                    EnableNativeEvents = false,
                    EnsureCleanSession = true
                };
                return options;
            }
        }

        public static DesiredCapabilities SetupInternetExplorer
        {
            // InternetExplorerOptions options = new InternetExplorerOptions();
            // options.IgnoreZoomLevel = true;
            // IWebDriver driver = new InternetExplorerDriver(options);
            get
            {
                var capability = DesiredCapabilities.InternetExplorer();
                capability.SetCapability("ensureCleanSession", true);
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static DesiredCapabilities SetupInternetExplorer9
        {
            get
            {
                var capability = DesiredCapabilities.InternetExplorer();
                capability.SetCapability(CapabilityType.Version, "9");
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static DesiredCapabilities SetupInternetExplorer8
        {
            get
            {
                var capability = DesiredCapabilities.InternetExplorer();
                capability.SetCapability(CapabilityType.Version, "8");
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static DesiredCapabilities SetupInternetExplorer7
        {
            get
            {
                var capability = DesiredCapabilities.InternetExplorer();
                capability.SetCapability(CapabilityType.Version, "7");
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static DesiredCapabilities SetupChrome
        {
            get
            {
                var capability = DesiredCapabilities.Chrome();
                capability.SetCapability(CapabilityType.IsJavaScriptEnabled, true);
                capability.SetCapability(CapabilityType.AcceptSslCertificates, true);
                capability.SetCapability(CapabilityType.HandlesAlerts, true);
                return capability;
            }
        }

        public static void ExecuteCommand(string command)
        {
            try
            {
                //  int exitCode;
                var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
                processInfo.CreateNoWindow = false;
                processInfo.UseShellExecute = false;
                // *** Redirect the output ***
                processInfo.RedirectStandardError = false;
                processInfo.RedirectStandardOutput = false;

                Process.Start(processInfo);
            }
            catch (Exception)
            {
            }
        }
    }

    public class BrowserConfig
    {
        public string Browser { get; set; }
        public string DriverType { get; set; }
        public string Url { get; set; }
    }
}