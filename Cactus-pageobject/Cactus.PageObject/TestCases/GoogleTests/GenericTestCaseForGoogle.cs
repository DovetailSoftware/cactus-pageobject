using Cactus.Drivers;
using Cactus.PageLogic.GooglePages;
using NUnit.Framework;
using NUnit_retry;

namespace Cactus.TestCases.GoogleTests
{
    [TestFixture, Category("GoogleSample")]
    [Retry(Times = 5, RequiredPassCount = 1)]
    public class GenericTestCaseForGoogle : WebDriverTestBase
    {
        #region Local Test Constructors and Setup

        [TestFixtureSetUp]
        public void LocalSetupTests()
        {

        }

        [TestFixtureTearDown]
        public void LocalTearDownTests()
        {

        }

        [SetUp]
        public void LocalSetupTest()
        {
        }

        [TearDown]
        public void LocalTeardownTest()
        {

        }
        #endregion

        [Test, Timeout(60000)]
        public void QueryGoogle()
        {
            using (var page = new GoogleSearchPage())
            {
                page.OpenPage();
                page.Query.SetTextValue("elephant");
                page.Query.SendKeys("ENTER");
                page.Body.Assert.ShouldContainText("large mammals");
            }
            Support.ScreenShot();
        }
    }
}
