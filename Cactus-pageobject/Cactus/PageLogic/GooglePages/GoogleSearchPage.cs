using System;
using Cactus.Drivers;
using How = Cactus.Drivers.PageObject.How;

namespace Cactus.PageLogic.GooglePages
{
    public class GoogleSearchPage : BasePageObject
    {
        #region Controls (PageObjects)

        public string Url = "https://www.google.com";
        public readonly Control Query = new Control(How.Name, "q");

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
