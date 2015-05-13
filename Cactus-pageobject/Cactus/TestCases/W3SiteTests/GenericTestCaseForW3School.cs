using Cactus.Drivers;
using Cactus.PageLogic.W3SitePages;
using NUnit.Framework;
using NUnit_retry;

namespace Cactus.TestCases.W3SiteTests
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
        public void SampleTagListPageSearch()
        {
            using (var page = new W3SitePage())
            {
                page.OpenPage();
                page.TagPageLink.Click();
                page.Body.Assert.ShouldContainText("HTML Tags Ordered Alphabetically");
            }
            Support.ScreenShot();
        }
    }
}
