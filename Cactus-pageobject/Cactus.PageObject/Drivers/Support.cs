﻿using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Cactus.Infrastructure;
using Microsoft.CSharp.RuntimeBinder;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Cactus.Drivers
{
    /// <summary>
    /// Many of these method helps are based on the OpenQA.Selenium.Support.UI  .dll  
    /// That is the reason for the name "Support" as the class name.
    /// </summary>
    public class Support
    {
        public static string ScreenShotFolder = @"c:\cactus_screenshots\";
        public const int DEFAULT_WAIT_SECONDS = 12;
        static ILogger _log;

        #region Screen Shot

        /// <summary>
        /// Grab a screen shot of the Browser 
        /// </summary>
        public static void ScreenShot()
        {
            try
            {
                _log = LogManager.GetLogger(null);

                if (Engine.WebDriver == null)
                {
                    throw new ArgumentException("Search context must be a RemoteWebDriver", "browserDriver");
                }

                var screenshot = ((ITakesScreenshot)Engine.WebDriver).GetScreenshot();

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-hhmm-ss");
                var screenShotFileName = "";
                if (Environment.GetEnvironmentVariable("isjenkins") != null)
                {
                    var jobName = Environment.GetEnvironmentVariable("JOB_NAME") ?? "local";

                    screenShotFileName =
                        $"{jobName.Replace("/BROWSER=Chrome,label=StoryTeller", "").Replace("/BROWSER=Firefox,label=StoryTeller", "")}_{Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1"}_{timestamp}_{Guid.NewGuid().ToString("N")}.png";
                }
                else
                {
                    screenShotFileName = $"{timestamp}_{Guid.NewGuid().ToString("N")}.png";
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["S3StorageUsage"]) &&
                    Environment.GetEnvironmentVariable("isjenkins") != null && (
                        Environment.GetEnvironmentVariable("PARENT_BUILD_NUMBER") != null ||
                        File.Exists(@"c:\parentBuildNumber.txt"))
                    )
                {
                    var regionName = Environment.GetEnvironmentVariable("SCREENSHOT_S3_REGION") ?? "us-west-2";
                    var bucketName = Environment.GetEnvironmentVariable("SCREENSHOT_S3_BUCKET") ??
                                     "jenkins-artifacts.us-west-2.dovetailnow.com";
                    var regionEndpoint = RegionEndpoint.GetBySystemName(regionName);
                    var baseS3Url = regionEndpoint.GetEndpointForService("s3");

                    var parentbuildNumber = Environment.GetEnvironmentVariable("PARENT_BUILD_NUMBER") ??
                                            File.ReadAllText(@"c:\parentBuildNumber.txt");
                    var s3Link =
                        $@"https://{baseS3Url}/{bucketName}/storyteller_screenshots/Blue/{parentbuildNumber.Trim()}/{
                            screenShotFileName}";
                    _log.Info($"ScreenShot file on s3:{Environment.NewLine}{s3Link}");

                    try
                    {
                        uploadToScreenshotToS3(screenshot, regionEndpoint, bucketName,
                            $"storyteller_screenshots/Blue/{parentbuildNumber.Trim()}/{screenShotFileName}");
                    }
                    catch (Exception s3Ex)
                    {
                        _log.Error("Could not upload screenshot to S3:", exception: s3Ex);
                    }
                }
                else
                {
                    Directory.CreateDirectory(ScreenShotFolder);
                    cleanupOldScreenShotFiles();
                    screenshot.SaveAsFile(ScreenShotFolder + screenShotFileName, System.Drawing.Imaging.ImageFormat.Png);
                    _log.Info(
                        string.Format("ScreenShot file saved locally as file:///" + ScreenShotFolder +
                                      screenShotFileName).Replace(@"\", "/"));
                }
            }
            catch (ArgumentException ar)
            {
                //ignore
                _log.Exception(ar);
                //System.ArgumentException: Search context must be a RemoteWebDriver Parameter name: browserDriver
            }
            catch (FileNotFoundException fileEx)
            {
                _log.Error("File not found for screenshot ", exception: fileEx);
            }
            catch (DirectoryNotFoundException dirEx)
            {
                _log.Error("Directory not found for screenshot ", exception: dirEx);
            }
            catch (IOException)
            {
                int winerr = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (winerr == 39 || winerr == 0x70)
                    throw new Exception("Disk is full, could not save any new screenshots");
                throw;
            }
            catch (WebDriverException)
            {
                //ignore. 
            }
            catch (Exception ex)
            {
                _log.Error("Error saving ScreenShot of Selenium Test ", exception: ex);
            }

        }

        static void uploadToScreenshotToS3(Screenshot screenshot, RegionEndpoint regionEndpoint, string bucketName, string keyName)
        {
            using (var amazonS3Client = new AmazonS3Client(regionEndpoint))
            {
                var utility = new TransferUtility(amazonS3Client);
                var uploadRequest = new TransferUtilityUploadRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    StorageClass = S3StorageClass.Standard,
                    InputStream = new MemoryStream(screenshot.AsByteArray)
                };
                utility.Upload(uploadRequest);
            }
        }

        private static void cleanupOldScreenShotFiles(int daysOld = 2)
        {
            var files = new DirectoryInfo(ScreenShotFolder).GetFiles("*.png");
            foreach (var file in files.Where(file => DateTime.UtcNow - file.CreationTimeUtc > TimeSpan.FromDays(daysOld)))
            {
                file.Delete();
            }
        }

        #endregion

        #region Wait Methods, each should overload with DEFAULT_WAIT_SECONDS

        /// <summary>
        /// This runs return jQuery.active == 0  Looking for jQuery to not be active
        /// Default is 8 seconds.
        /// </summary>
        public static void WaitForAjaxToComplete()
        {
            WaitForAjaxToComplete(TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// This runs return jQuery.active == 0  Looking for jQuery to not be active
        /// </summary>
        public static void WaitForAjaxToComplete(TimeSpan waitTimeSpan)
        {
            try
            {
                var currentUrl = Engine.WebDriver.Url;
                if (currentUrl.Contains("login")
                    || currentUrl.Contains(".htm")
                    || currentUrl.EndsWith("gif")
                    || currentUrl.Contains("/support/")
                    || currentUrl.Contains("/agent/console/cases"))
                    return;
            }
            catch (UnhandledAlertException)
            {
                //ignore
            }

            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitForAjaxToComplete", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );

                wait.Until(d =>
                {
                    try
                    {
                        var javaScriptExecutor = Engine.WebDriver as IJavaScriptExecutor;
                        var ajaxIsComplete = javaScriptExecutor != null
                                             && (bool)javaScriptExecutor.ExecuteScript("return jQuery.active == 0");
                        if (ajaxIsComplete)
                            return true;

                        Thread.Sleep(50);
                        return false;
                    }
                    catch (InvalidOperationException)
                    {
                        // if a page does not have jquery it will hit this exception.
                        return true;
                    }
                    catch (TimeoutException)
                    {
                        // ignore and try and continue.
                        return true;
                    }
                });
            }
        }

        /// <summary>
        /// Wait for set time. Also make sure page returns to readyState. 
        /// Default time wait = 8 seconds
        /// </summary>
        public static void WaitForPageReadyState()
        {
            WaitForPageReadyState(TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait for set time. Also make sure page returns to readyState. 
        /// Default time wait = 8 seconds
        /// </summary>
        public static void WaitForAlliFramesPageReadyState(bool swithToFrameZero = true)
        {
            Engine.SwitchOutToDefaultContent();
            WaitForPageReadyState(TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
            if (Engine.HasiFrame)
            {
                foreach (var frame in Engine.IframesCollection)
                {
                    Engine.SwitchToiFrame(frame);
                    WaitForPageReadyState(TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
                }
            }
            else
            {
                Debug.WriteLine("No Iframes on page");
            }
            Engine.SwitchOutToDefaultContent();
            Engine.SwitchToiFrame(0);
        }

        /// <summary>
        /// Wait for set time. Also make sure page returns to readyState.
        /// Time is in seconds.
        /// </summary>
        /// <param name="seconds"></param>
        public static void WaitForPageReadyState(double seconds)
        {
            WaitForPageReadyState(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Wait for set time. Also make sure page returns to readyState.
        /// </summary>
        /// <param name="waitTimeSpan"></param>
        public static void WaitForPageReadyState(TimeSpan waitTimeSpan)
        {
            try
            {
                var currentUrl = Engine.WebDriver.Url;
                if (currentUrl.Contains("login")
                    || currentUrl.Contains(".htm")
                    || currentUrl.EndsWith("gif")
                    || currentUrl.Contains("/support/"))
                    return;
            }
            catch (UnhandledAlertException)
            {
                //ignore
            }
            catch (NoSuchWindowException)
            {
                return;
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitForPageReadyState", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );

                var javascript = Engine.WebDriver as IJavaScriptExecutor;
                if (javascript == null)
                    throw new ArgumentException("Driver must support javascript execution");

                try
                {
                    if (Engine.FindElement(By.TagName("body"), safeMode: true) == null)
                    {
                        Engine.SwitchOutToDefaultContent();
                    }
                }
                catch (Exception)
                {
                    Engine.SwitchOutToDefaultContent();
                }

                if (Engine.Browser == SupportedBrowserType.Firefox)
                    Thread.Sleep(20);

                wait.Until(d =>
                {
                    try
                    {
                        var readyState = javascript.ExecuteScript(
                            "if (document.readyState) return document.readyState;").ToString();
                        return readyState.ToLower() == "complete";
                    }
                    catch (SystemException)
                    {
                        Engine.SwitchOutToDefaultContent();
                        return true;
                    }
                    catch (NoSuchWindowException)
                    {
                        Engine.WebDriver.SwitchTo().Window(Engine.WebDriver.WindowHandles.Last());
                        return true;
                    }
                    catch (NoSuchFrameException)
                    {
                        Engine.SwitchOutToDefaultContent();
                        return true;
                    }
                    catch (WebDriverException webDriverException)
                    {
                        if (webDriverException.Message.Contains("aborted"))
                        {
                            Debug.WriteLine(
                                "ERROR: Screen tear down showed message, the thread was being aborted on WaitForPageLoad.");
                        }
                        if (Engine.WebDriver.WindowHandles.Count == 1)
                        {
                            // go back to parent window.
                            Engine.WebDriver.SwitchTo().Window(Engine.WebDriver.WindowHandles[0]);
                        }
                        return true;
                    }
                    catch
                    {
                        Engine.SwitchOutToDefaultContent();
                        return true;
                    }
                });


                if (Engine.Browser == SupportedBrowserType.Firefox)
                    Support.WaitForAjaxToComplete(waitTimeSpan);
            }
        }

        /// <summary>
        /// Wait for Text to be shown on the page source.
        /// Default time wait = 8 seconds
        /// </summary>
        /// <param name="text"></param>
        public static void PageWaitTextAvailable(string text)
        {
            PageWaitTextAvailable(text, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait for Text to be shown on the page source
        /// </summary>
        /// <param name="seconds">Seconds to wait</param>
        /// <param name="text"></param>
        public static void PageWaitTextAvailable(double seconds, string text)
        {
            PageWaitTextAvailable(text, TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Wait for Text to be shown on the page source
        /// </summary>
        /// <param name="waitTimeSpan">Timespan to wait</param>
        /// <param name="text"></param>
        public static void PageWaitTextAvailable(string text, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("PageWaitTextAvailable", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                wait.Until(driver => Engine.WebDriver.PageSource.Contains(text));
            }
        }

        /// <summary>
        /// Wait for the Url To contain a string anywhere in the Url object
        /// Default wait = 8 seconds
        /// </summary>
        /// <param name="expected"></param>
        public static void WaitForUrlToContain(string expected)
        {
            WaitForUrlToContain(expected, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait for the Url To contain a string anywhere in the Url object
        /// </summary>
        /// <param name="waitTimeSpan"></param>
        /// <param name="expected"></param>

        public static void WaitForUrlToContain(string expected, TimeSpan waitTimeSpan)
        {
            if (string.IsNullOrEmpty(expected))
                throw new Exception("WaitForUrlToContain: expected string was empty");
            _log = LogManager.GetLogger(null);

            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitForUrlToContain", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );

                try
                {
                    wait.Until(driver =>
                    {
                        try
                        {
                            if (!Engine.WebDriver.Url.ToLower().Contains(expected.ToLower())) return false;

                            _log = LogManager.GetLogger(null);
                            _log.Debug("Current URL = " + Engine.WebDriver.Url);
                            WaitForPageReadyState(waitTimeSpan);
                            return true;
                        }
                        catch (NoSuchWindowException)
                        {
                            return false;
                        }
                    });
                }

                catch (WebDriverTimeoutException timeoutException)
                {
                    _log.Error("FAIL: WaitForUrlToContain ", timeoutException);
                }
                catch (Exception ex)
                {
                    _log.Error("FAIL: WaitForUrlToContain " + expected, ex);
                }

            }
        }

        /// <summary>
        /// Wait for the URL to End with a string.
        /// Default wait time 8 seconds
        /// </summary>
        /// <param name="expected"></param>
        public static void WaitForUrlToEndWith(string expected)
        {
            WaitForUrlToEndWith(expected, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait for the URL to End with a string.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="waitTimeSpan"></param>
        public static void WaitForUrlToEndWith(string expected, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitForUrlToEndWith", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                if (expected.Contains(";"))
                {
                    wait.Until(driver =>
                    {
                        if (expected.Split(';').Any(expec => Engine.WebDriver.Url.ToLower().EndsWith(expec.ToLower())))
                        {
                            Debug.WriteLine(Engine.WebDriver.Url);
                            WaitForPageReadyState(waitTimeSpan);
                            return true;
                        }
                        return false;
                    });
                }
                else
                {
                    wait.Until(driver =>
                    {
                        if (Engine.WebDriver.Url.ToLower().EndsWith(expected.ToLower()))
                        {
                            Debug.WriteLine(Engine.WebDriver.Url);
                            WaitForPageReadyState(waitTimeSpan);
                            return true;
                        }
                        return false;
                    });
                }
            }
        }

        /// <summary>
        /// Wait until an alert is present on page
        /// Default wait time 12 seconds
        /// </summary>
        public static void WaitUntilAlertIsPresent()
        {
            WaitUntilAlertIsPresent(TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until an alert is present on page
        /// </summary>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static void WaitUntilAlertIsPresent(TimeSpan waitTimeSpan)
        {
            _log = LogManager.GetLogger(null);

            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilAlertIsPresent ", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.AlertIsPresent());
                }
                catch (WebDriverTimeoutException)
                {
                    _log.Error("Alert was never made present. TimeoutException");
                    ScreenShot();
                }
                catch (TimeoutException)
                {
                    _log.Error("Alert was never made present. TimeoutException");
                    ScreenShot();
                }
            }
        }

        /// <summary>
        /// Wait until an alert is in the correct state
        /// Default wait time 12 seconds
        /// </summary>
        /// <param name="state"></param>
        public static void WaitUntilAlertState(bool state)
        {
            WaitUntilAlertState(state, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until an alert is in the correct state
        /// </summary>
        /// <param name="state"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static void WaitUntilAlertState(bool state, TimeSpan waitTimeSpan)
        {
            _log = LogManager.GetLogger(null);

            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilAlertState ", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.AlertState(state));
                }
                catch (WebDriverTimeoutException)
                {
                    _log.Error("Alert was never in the correct state. TimeoutException");
                    ScreenShot();
                }
                catch (TimeoutException)
                {
                    _log.Error("Alert was never in the correct state. TimeoutException");
                    ScreenShot();
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="parentElement"></param>
        /// <param name="childBy">By.Id("hello")</param>
        /// <returns></returns>
        public static bool WaitUntilChildElementIsPresent(IWebElement parentElement, By childBy)
        {
            return WaitUntilChildElementIsPresent(parentElement, childBy, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="element"></param>
        /// <param name="childBy"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilChildElementIsPresent(IWebElement element, By childBy, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilChildElementsArePresent " + childBy, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => element.FindElements(childBy).Count > 0);
                    return Engine.IsElementPresent(element);
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="parentElement"></param>
        /// <param name="childBy">By.Id("hello")</param>
        /// <returns></returns>
        public static bool WaitUntilChildElementIsPresent(IWebElement parentElement, OpenQA.Selenium.By childBy)
        {
            return WaitUntilChildElementIsPresent(parentElement, childBy, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="element"></param>
        /// <param name="childBy"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilChildElementIsPresent(IWebElement element, OpenQA.Selenium.By childBy, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilChildElementsArePresent " + childBy, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => element.FindElements(childBy).Count > 0);
                    return Engine.IsElementPresent(element);
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="parentElement"></param>
        /// <param name="childBy">By.Id("hello")</param>
        /// <param name="countGreaterThan"></param>
        /// <returns></returns>
        public static bool WaitUntilChildElementsArePresent(IWebElement parentElement, OpenQA.Selenium.By childBy, int countGreaterThan = 0)
        {
            return WaitUntilChildElementsArePresent(parentElement, childBy, countGreaterThan, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="element"></param>
        /// <param name="childBy"></param>
        /// <param name="countGreaterThan"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilChildElementsArePresent(IWebElement element, OpenQA.Selenium.By childBy, int countGreaterThan, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilChildElementsArePresent " + childBy, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => element.FindElements(childBy).Count > countGreaterThan);
                    return Engine.IsElementPresent(element);
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool WaitUntilElementIsPresent(IWebElement element)
        {
            return WaitUntilElementIsPresent(element, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(By by)
        {
            return WaitUntilElementIsPresent(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(OpenQA.Selenium.By by)
        {
            return WaitUntilElementIsPresent(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }


        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="element"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsPresent(IWebElement element, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsPresent ", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => Engine.IsElementPresent(element));
                    return Engine.IsElementPresent(element);
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(By by, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsPresent " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => Engine.IsElementPresent(@by));
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                throw new Exception("No by statement was given for this wait call.");
            }
            _log = LogManager.GetLogger(null);

            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsPresent " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(@by));
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    _log = LogManager.GetLogger(null);
                    _log.Error(by + " was never made present. TimeoutException");
                    ScreenShot();
                    return null;
                }
                catch (TimeoutException)
                {
                    _log = LogManager.GetLogger(null);
                    _log.Error(by + " was never made present. TimeoutException");
                    ScreenShot();
                    return null;
                }
                catch (NoSuchElementException)
                {
                    _log = LogManager.GetLogger(null);
                    _log.Error(by + " was never made present. NoSuchElementException");
                    ScreenShot();
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page, then wait for class
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="classString"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementHasClass(OpenQA.Selenium.By by, string classString, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                return null;
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementHasClass " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(@by));
                    wait.Until(d => Engine.FindElement(@by).GetAttribute("class").Contains(classString));
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page, then wait for text
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="text"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementHasText(OpenQA.Selenium.By by, string text, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                return null;
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementHasText " + by + " " + text, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(@by));
                    if (string.IsNullOrEmpty(text))
                    {
                        wait.Until(d => Engine.FindElement(@by).Text.Length > 0);
                    }
                    else
                    {
                        wait.Until(d => Engine.FindElement(@by).Text.Contains(text) || (Engine.FindElement(@by).GetAttribute("value")?.Contains(text) ?? false));
                    }

                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page, then wait for text
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="text"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementDoesNotHaveText(OpenQA.Selenium.By by, string text, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                return null;
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementDoesNotHaveText " + by + " " + text, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(@by));
                    if (string.IsNullOrEmpty(text))
                    {
                        wait.Until(d => Engine.FindElement(@by).Text.Length > 0);
                    }
                    else
                    {
                        wait.Until(d => !Engine.FindElement(@by).Text.Contains(text));
                    }

                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(CustomBy by)
        {
            return WaitUntilElementIsPresent(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsPresent(CustomBy by, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsPresent " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d =>
                    {
                        return Engine.IsElementPresent(by);
                    });
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is Visible on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsNotVisible(By by)
        {
            return WaitUntilElementIsNotVisible(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is Visable on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsNotVisible(OpenQA.Selenium.By by)
        {
            return WaitUntilElementIsNotVisible(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is Not Visible on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsNotVisible(By by, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                throw new Exception("No By was given to WaitUntilElementIsNotVisible");
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsNotVisible " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(
                        d =>
                            (Engine.FindElement(@by, safeMode: true) == null) ||
                            Engine.FindElement(@by, safeMode: true).Displayed == false);
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return true;
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Wait until the element is Not Visible on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsNotVisible(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                throw new Exception("No By was given to WaitUntilElementIsNotVisible");
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsNotVisible " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d =>
                    {
                        try
                        {
                            if (Engine.FindElement(@by, safeMode: true) != null)
                                return Engine.FindElement(@by, safeMode: true).Displayed == false;
                            return true;
                        }
                        catch (StaleElementReferenceException)
                        {
                            return true;
                        }
                        catch (NullReferenceException)
                        {
                            return true;
                        }
                    });
                    return true;
                }
                catch (OpenQA.Selenium.InvalidSelectorException)
                {
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return true;
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static bool WaitUntilElementIsVisible(IWebElement element)
        {
            return WaitUntilElementIsVisible(element, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is Visible on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsVisible(By by)
        {
            return WaitUntilElementIsVisible(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait until the element is Visable on page
        /// Default wait time 8 seconds.
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsVisible(OpenQA.Selenium.By by)
        {
            return WaitUntilElementIsVisible(by, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }


        /// <summary>
        /// Wait until the element is Visable on page
        /// </summary>
        /// <param name="element"></param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static bool WaitUntilElementIsVisible(IWebElement element, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsVisible ", timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(d => Engine.IsElementVisible(element));
                    return Engine.IsElementVisible(element);
                }
                catch (WebDriverTimeoutException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// Wait until the element is Visable on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsVisible(By by, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsVisible " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementIsVisible(@by));
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Wait until the element is present on page
        /// </summary>
        /// <param name="by">By.Id("hello")</param>
        /// <param name="waitTimeSpan">Timeout in TimeSpan</param>
        /// <returns></returns>
        public static IWebElement WaitUntilElementIsVisible(OpenQA.Selenium.By by, TimeSpan waitTimeSpan)
        {
            if (@by == null)
            {
                return null;
            }
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitUntilElementIsVisible " + by, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );
                try
                {
                    wait.Until(ExpectedConditions.ElementIsVisible(@by));
                    return Engine.FindElement(by);
                }
                catch (WebDriverTimeoutException)
                {
                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }



        /// <summary>
        /// Wait for a element i.e. button to have IsEnabled = false.
        /// Default wait time is 8 seconds
        /// </summary>
        /// <param name="button"></param>
        public static void WaitForButtonToBeDisabled(Control button)
        {
            WaitForButtonToBeDisabled(button, TimeSpan.FromSeconds(DEFAULT_WAIT_SECONDS));
        }

        /// <summary>
        /// Wait for a element i.e. button to have IsEnabled = false.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="waitTimeSpan"></param>
        public static void WaitForButtonToBeDisabled(Control button, TimeSpan waitTimeSpan)
        {
            using (PerformanceTimer.Start(
                timer => PerformanceTimer.LogTimeResult("WaitForButtonToBeDisabled " + button.MyBy, timer)))
            {
                IClock iClock = new SystemClock();
                var wait = new WebDriverWait(clock: iClock,
                    driver: Engine.WebDriver,
                    timeout: waitTimeSpan,
                    sleepInterval: TimeSpan.FromMilliseconds(5)
                    );

                var javascript = Engine.WebDriver as IJavaScriptExecutor;
                if (javascript == null)
                    throw new ArgumentException("Driver must support javascript execution");

                wait.Until(d =>
                {
                    try
                    {
                        return !button.IsEnabled;
                    }
                    catch (InvalidOperationException invalidOperationException)
                    {
                        //Window is no longer available
                        return invalidOperationException.Message.ToLower().Contains("unable to get browser");
                    }
                    catch (WebDriverException webDriverException)
                    {
                        //Browser is no longer available
                        return webDriverException.Message.ToLower().Contains("unable to connect");
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
        }
        #endregion

        public static string GetParentBuildNumber()
        {
            try
            {
                return Environment.GetEnvironmentVariable("PARENT_BUILD_NUMBER") ?? File.ReadAllText(@"c:\parentBuildNumber.txt");
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// This method will use javaScript to return an absolute XPATH locater based on an IWebElement
        /// Usage: WebElement search = Engine.WebDriver.FindElement(By.name("q"));			
        /// Engine.WebDriver.FindElement(By.xpath(GenerateXpathFromElement(element))).SendKeys("turkey");
        /// </summary>
        /// <param name="element">Element already shown on page</param>
        /// <returns></returns>
        public static string GenerateXpathFromElement(IWebElement element)
        {

            var script =
                "function absoluteXPath(element) {" +
                "var comp, comps = [];" +
                "var parent = null;" +
                "var xpath = '';" +
                "var getPos = function(element) {" +
                "var position = 1, curNode;" +
                "if (element.nodeType == Node.ATTRIBUTE_NODE) {" +
                "return null;" +
                "}" +
                "for (curNode = element.previousSibling; curNode; curNode = curNode.previousSibling) {" +
                "if (curNode.nodeName == element.nodeName) {" +
                "++position;" +
                "}" +
                "}" +
                "return position;" +
                "};" +

                "if (element instanceof Document) {" +
                "return '/';" +
                "}" +

                "for (; element && !(element instanceof Document); element = element.nodeType == Node.ATTRIBUTE_NODE ? element.ownerElement : element.parentNode) {" +
                "comp = comps[comps.length] = {};" +
                "switch (element.nodeType) {" +
                "case Node.TEXT_NODE:" +
                "comp.name = 'text()';" +
                "break;" +
                "case Node.ATTRIBUTE_NODE:" +
                "comp.name = '@' + element.nodeName;" +
                "break;" +
                "case Node.PROCESSING_INSTRUCTION_NODE:" +
                "comp.name = 'processing-instruction()';" +
                "break;" +
                "case Node.COMMENT_NODE:" +
                "comp.name = 'comment()';" +
                "break;" +
                "case Node.ELEMENT_NODE:" +
                "comp.name = element.nodeName;" +
                "break;" +
                "}" +
                "comp.position = getPos(element);" +
                "}" +

                "for (var i = comps.length - 1; i >= 0; i--) {" +
                "comp = comps[i];" +
                "xpath += '/' + comp.name.toLowerCase();" +
                "if (comp.position !== null) {" +
                "xpath += '[' + comp.position + ']';" +
                "}" +
                "}" +

                "return xpath;" +

                "} return absoluteXPath(arguments[0]);";

            return Engine.Execute<string>(script, element);

        }

        public static string[] ListOfAttributeValues(ReadOnlyCollection<IWebElement> elementCollection, string attributeName)
        {
            return elementCollection.Select(element => element.GetAttribute(attributeName)).ToArray();
        }

        public static bool IsNumber(String value)
        {
            return value.ToCharArray().Where(x => !Char.IsDigit(x)).Count() == 0;
        }

        /// <summary>
        /// Returns a value indicating whether the specified System.String object occurs within the body tag
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsTextPreset(string text)
        {
            return Engine.WebDriver.FindElement(OpenQA.Selenium.By.TagName("body")).Text.Contains(text);
        }

        /// <summary>
        /// Look for Error 500, look for Alert 
        /// </summary>
        /// <returns></returns>
        public static bool DetectErrors()
        {
            try
            {
                if ((Engine.WebDriver.FindElement(OpenQA.Selenium.By.TagName("body")).Text.Contains("500 error")) ||
                    (Engine.WebDriver.FindElement(OpenQA.Selenium.By.TagName("body")).Text.Contains("Server Error in '/'")))
                    return true;
            }
            catch
            {
                return false;
            }
            return false;
        }


        /// <summary>
        /// Retrieve the count(int) of all of the elements found with the given xPath.
        /// </summary>
        /// <param name="xPath"></param>
        /// <returns>Count of items on page</returns>
        public static int GetXpathCount(string xPath)
        {
            int count;
            try
            {
                count = Engine.WebDriver.FindElements(OpenQA.Selenium.By.XPath(xPath)).Count;
            }
            catch
            {
                count = 0;
            }

            return count;
        }

        /// <summary>
        /// Generate a random string value, input the length of the integer passed to it
        /// </summary>
        /// <param name="stringSeed">length of string needed</param>
        /// <returns>string</returns>
        public static string GenerateRandomString(int stringSeed)
        {

            var builder = new StringBuilder();
            var random = new Random();
            char ch;
            for (int i = 0; i < stringSeed; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }


        /// <summary>
        /// Using Selenium, drops from one element to another element.
        /// </summary>
        /// <param name="dragSelector"></param>
        /// <param name="dropSelector"></param>
        public static void DragAndDrop(Control dragSelector, Control dropSelector)
        {
            var actions = new Actions(Engine.WebDriver);
            IWebElement element1 = dragSelector.Element;
            IWebElement element2 = dropSelector.Element;
            actions.DragAndDrop(element1, element2).Perform();
            actions.ContextClick(element2).Perform();
        }

        /// <summary>
        /// Using Selenium, drops from one element to another element.
        /// </summary>
        /// <param name="dragSelector"></param>
        /// <param name="dropSelector"></param>
        public static void DragAndDrop(IWebElement dragSelector, IWebElement dropSelector)
        {
            var actions = new Actions(Engine.WebDriver);
            actions.DragAndDrop(dragSelector, dropSelector).Perform();
            actions.ContextClick(dropSelector).Perform();
        }


        /// <summary>
        /// this can be used to find the property of a dynamic object.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static object GetDynamicSubProperty(object o, string member)
        {
            if (o == null) throw new ArgumentNullException("o");
            if (member == null) throw new ArgumentNullException("member");
            Type scope = o.GetType();
            IDynamicMetaObjectProvider provider = o as IDynamicMetaObjectProvider;
            if (provider != null)
            {
                ParameterExpression param = Expression.Parameter(typeof(object));
                DynamicMetaObject mobj = provider.GetMetaObject(param);
                GetMemberBinder binder = (GetMemberBinder)Microsoft.CSharp.RuntimeBinder.Binder.GetMember(0, member, scope, new CSharpArgumentInfo[] { CSharpArgumentInfo.Create(0, null) });
                DynamicMetaObject ret = mobj.BindGetMember(binder);
                BlockExpression final = Expression.Block(
                    Expression.Label(CallSiteBinder.UpdateLabel),
                    ret.Expression
                );
                LambdaExpression lambda = Expression.Lambda(final, param);
                Delegate del = lambda.Compile();
                return del.DynamicInvoke(o);
            }
            else
            {
                return o.GetType().GetProperty(member, BindingFlags.Public | BindingFlags.Instance).GetValue(o, null);
            }
        }

        /// <summary>
        ///   Method verifies that the current browser Title contains the string and returns
        ///   true or false depending on the state
        ///    javascript: return document.title
        /// </summary>
        /// <param name="titleContains"></param>
        /// <returns></returns>
        public static bool VerifyPageTitle(string titleContains)
        {

            var title =
                 Engine.Execute<string>(Engine.WebDriver, string.Format("return document.title"));
            return title.Contains(titleContains);
        }

        /// <summary>
        ///   Pass this method a string (e.g. the endpoint of a URI of an MVC app)
        ///   Method verifies that the current browser URL contains the string and returns
        ///   true or false depending on the state
        /// </summary>
        /// <param name="urlString"></param>
        /// <returns></returns>
        public static bool VerifyCurrentUrl(string urlString)
        {
            return Engine.WebDriver.Url.Contains(urlString);
        }

        /// <summary>
        ///   Pass this method a string (e.g. the endpoint of a URI of an MVC app)
        ///   Method verifies that the current browser URL contains the string and returns
        ///   true or false depending on the state
        /// </summary>
        /// <param name="urlPartialString"></param>
        /// <returns></returns>
        public static bool VerifyCurrentUrlContains(string urlPartialString)
        {
            var currentUrl = Engine.WebDriver.Url;
            return currentUrl.ToLower().Contains(urlPartialString.ToLower());
        }

        public static string GetPublicIP()
        {
            String direction = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                direction = stream.ReadToEnd();
            }

            //Search for the ip in the html
            int first = direction.IndexOf("Address: ") + 9;
            int last = direction.LastIndexOf("</body>");
            direction = direction.Substring(first, last - first);

            return direction;
        }
    }
}