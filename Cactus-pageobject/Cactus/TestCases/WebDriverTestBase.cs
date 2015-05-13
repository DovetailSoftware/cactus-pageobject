using Cactus.Drivers;
using NUnit.Framework;

namespace Cactus.TestCases
{
    /// <summary>
    /// When inherited by A test class, this will automatically handle Start up and shutdown of the default browser.
    /// Inherit the "TestBase" class instead if you want to manually startup/shutdown in your tests.
    /// </summary>
    [TestFixture]
    public abstract class WebDriverTestBase : TestBase
    {

        [TestFixtureSetUp]
        public new void SetupTests()
        {
            // This will use the defaulted BrowserType
            Engine.InitializeBrowserInstance();
        }

        [TestFixtureTearDown]
        public void TearDownTests()
        {
            Engine.ShutDown();
        }

        [SetUp]
        public new void SetupTest()
        {
            Engine.EnsureBrowserIsLaunched();
            //Engine.RefreshPage();
        }

        protected override void OnTestFailed()
        {
            Support.ScreenShot();
            // it is advisable to "logout" of your app.  
        }

    }
}
