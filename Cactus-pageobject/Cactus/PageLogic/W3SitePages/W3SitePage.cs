using System;
using Cactus.Drivers;
using How = Cactus.Drivers.PageObject.How;

namespace Cactus.PageLogic.W3SitePages
{
    public class W3SitePage : BasePageObject
    {
        #region Controls (PageObjects)

        public string Url = "http://www.w3schools.com/html/default.asp";
        public readonly Control TagPageLink = new Control(How.HtmlTag, "href", "/tags/default.asp");

        #endregion

        #region Page Methods / Test Steps

        public void OpenPage()
        {
            Engine.GoToUrl(Url);
            Support.WaitForUrlToContain(Url, TimeSpan.FromSeconds(30));
        }

            #region Enter Data Methods

            #endregion

            #region Select / Dropdown Methods
        
            #endregion

            #region Verify Data Methods
        
            #endregion

            #region Get Field Data Methods

            #endregion

            #region Click Methods

            #endregion

        #endregion
    }
}
