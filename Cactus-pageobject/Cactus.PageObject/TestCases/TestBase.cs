using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Cactus.Drivers;
using Cactus.Infrastructure;
using Cactus.Infrastructure.NStatsD;
using NUnit.Framework;
using Should;

namespace Cactus.TestCases
{
    [TestFixture]
    public abstract class TestBase
    {
        readonly ILogger _log;
        public ILogger Log { get { return _log; } }
        readonly Stopwatch _stopwatch = new Stopwatch();

        protected TestBase()
        {
            _log = new UxTestingLogger();
        }

        protected virtual void setupBeforeTimer()
        {
            
        }

        [TestFixtureSetUp]
        public void SetupTests()
        {
            
        }

        [SetUp]
        public void SetupTest()
        {
            _log.Info(string.Format("-======================================================-"
                                       + Environment.NewLine + "Test Case: [{0}] is being started.", TestContext.CurrentContext.Test.Name));

            setupBeforeTimer();

            // TIME EACH TEST CASE
            _stopwatch.Start();
        }

        protected virtual void OnTestFailed()
        {
        }

        protected virtual void OnTestPassed()
        {
        }

        protected virtual void OnTestInconclusive()
        {
        }

        protected virtual void OnTestSkipped()
        {
        }

        [TearDown]
        public void TeardownTest()
        {
            _stopwatch.Stop();

            try
            {
                var key = "testing.ui.testcase.machine." + Environment.MachineName + ".testname." +
                          TestContext.CurrentContext.Test.Name;
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["NstatUsage"]))
                {
                    Client.Current.Timing(key, _stopwatch.Elapsed.TotalMilliseconds);
                }
                var addon = "";
                switch (TestContext.CurrentContext.Result.Status)
                {
                    case TestStatus.Failed:
                        addon = "ⓍＸⓍＸⓍＸⓍＸⓍＸⓍxFailedxⓍＸⓍＸⓍＸⓍＸⓍＸⓍＸⓍＸⓍ";
                        OnTestFailed();
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["NstatUsage"]))
                        {
                            Client.Current.Increment(key + ".failed");
                        }
                        break;
                    case TestStatus.Passed:
                        addon = "✓+✓+✓+✓+✓+✓+✓+✓+✓Passed✓+✓+✓+✓+✓+✓+✓+✓+✓+✓+✓+✓+";
                        OnTestPassed();
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["NstatUsage"]))
                        {
                            Client.Current.Increment(key + ".passed");  
                        }
                        break;
                    case TestStatus.Inconclusive:
                        addon = "????????????????????Inconclusive????????????????????????";
                        OnTestInconclusive();
                        break;
                    case TestStatus.Skipped:
                        addon = "sSSSSSSSSSSSSSSSSSSSssSkippedssSSSSSSSSSSSSSSSSSSSSSSSSs";
                        OnTestSkipped();
                        break;
                }

                _log.Info(string.Format(
                    "-^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^-" + Environment.NewLine +
                    addon + Environment.NewLine
                    + "Test Case: [{0}] has ended: Status: {1} , State: {2} {3} and took seconds {4}.{5}{3}, {6}",
                    TestContext.CurrentContext.Test.Name,
                    TestContext.CurrentContext.Result.Status,
                    TestContext.CurrentContext.Result.State,
                    Environment.NewLine,
                    _stopwatch.Elapsed.TotalSeconds,
                    _stopwatch.Elapsed.Milliseconds,
                    DateTime.Now));

                var category = (ArrayList) TestContext.CurrentContext.Test.Properties["_CATEGORIES"];
                if (category.Contains("Acceptance") &&
                    TestContext.CurrentContext.Result.Status.Equals(TestStatus.Failed))
                {
                    Assert.Ignore("Ignoring failure for Acceptance Test: [{0}]", TestContext.CurrentContext.Test.Name);
                }
            }
            finally
            {
                _stopwatch.Reset();
 
            }
        }

        /// <summary>
        /// This does a Thread.Sleep for X Milliseconds
        /// </summary>
        /// <param name="milliSecondsToPause"></param>
        public void Pause(int milliSecondsToPause)
        {
            _log.Info($"Pausing for: {milliSecondsToPause} milliseconds");
            Thread.Sleep(milliSecondsToPause);
            Support.WaitForPageReadyState();
        }
    }
}