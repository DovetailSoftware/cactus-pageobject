using System.Text;
using OpenQA.Selenium;


namespace Cactus.Drivers
{
    public class CustomBy : OpenQA.Selenium.By
    {
        /// <summary>
        /// CustomBy extension for By.  Uses a CssSelector to find tags with data-automate as the attribute.  
        /// Expects a value assigned to the tag to be entered
        /// </summary>
        /// <param name="customByString"></param>
        public CustomBy(string customByString)
        {
            FindElementMethod = context =>
            {
                IWebElement mockElement = Engine.WebDriver.FindElement(CssSelector("[data-automate=\"" + customByString + "\"]"));
                return mockElement;
            };
        }

        /// <summary>
        /// Custom By statement for specific DataSupapicka to be specified.  Extends XPATH
        ///  ////*[contains(@data-supapicka,'asset-employee')]
        /// </summary>
        /// <param name="value">value to search for</param>
        /// <returns></returns>
        public static OpenQA.Selenium.By DataSupapicka(string value)
        {
            return XPath("//*[contains(@data-supapicka,'" + value + "')]");
        }

        /// <summary>
        /// Custom By statement for specific SRC to be specified.  Extends XPATH
        /// </summary>
        /// <param name="value">value to search for</param>
        /// <returns></returns>
        public static OpenQA.Selenium.By Src(string value)
        {
            return XPath("//*[contains(@src,'" + value + "')]");
        }

        /// <summary>
        /// Custom By statement for specific attribute to be specified.  Extends CssSelector
        /// </summary>
        /// <param name="tagname">attribute to search for</param>
        /// <param name="value">value to search for</param>
        /// <returns></returns>
        public static OpenQA.Selenium.By CssTag(string tagname, string value)
        {
            return CssSelector("[" + tagname + "=\"" + value + "\"]");
        }

        /// <summary>
        /// Custom By statement for specific attribute to be specified.  Extends XPATH
        /// </summary>
        /// <param name="tagname">attribute to search for</param>
        /// <param name="value">value to search for</param>
        /// <returns></returns>
        public static OpenQA.Selenium.By HtmlTag(string tagname, string value)
        {
            if (tagname == "text")
            {
                tagname = tagname + "()";
            }
            else
            {
                tagname = "@" + tagname;
            }
            return XPath("//*[contains(" + tagname + ",'" + value + "')]");
        }

        /// <summary>
        /// Custom By statement for finding a element by value.  Extends CssSelector
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static OpenQA.Selenium.By CssValue(string text)
        {
            return CssSelector("[value=\"" + text + "\"]");
        }

       
        /// <summary>
        /// CustomBy extension for jquery By.  Uses a jquery selector to find DOM.  Will send this to Browser $("div#item1").xpath(); To get back DOM xpath, convert to Selenium
        /// </summary>
        /// <param name="selector"></param>
        public static OpenQA.Selenium.By jQuery(string selector)
        {
            //format selector to add 
            var jQueryCommand = "$(\"" + selector + "\").xpath();";
            //$("div#item1").xpath();
            var xpath = Engine.Execute<string>(Engine.WebDriver, jqueryXpathBuilder() + " " + jQueryCommand);
            //note: all javascript has to run in same process/time.  If you try and split it you will not see data.
            // this is a selenium thing, where it returns the page back to normal after javascript run.
            return XPath(xpath);
            
        }

        // http://edudotnet.blogspot.com/2013/08/get-element-xpath-of-selected-element.html
        // alternative = 
        private static string jqueryXpathBuilder()
        {
            var sb = new StringBuilder();
            sb.Append("jQuery.fn.extend({   ");
            sb.Append(" xpath: function(){  ");
            sb.Append(" path = new Array(); ");
            sb.Append( "for(var i in this){ ");
            sb.Append("  var elt = this[i]; ");
            sb.Append("  if (elt && elt.id){ ");
            sb.Append("    path.push('//' + elt.tagName.toLowerCase() + '[id=' + elt.id + ']'); ");
            sb.Append("  ");
            sb.Append("  } else { ");
            sb.Append("    var tpath = ''; ");
            sb.Append("    for (; elt && elt.nodeType == 1; elt = elt.parentNode){ ");
            sb.Append("     idx = 1; ");
            sb.Append("      for (var sib = elt.previousSibling; sib ; sib = sib.previousSibling) ");
            sb.Append("          if(sib.nodeType == 1 && sib.tagName == elt.tagName) idx++; ");
            sb.Append("      xname = elt.tagName.toLowerCase(); ");
            sb.Append("      if (idx > 1) ");
            sb.Append("        xname += '[' + idx + ']'; ");
            sb.Append("      tpath = '/' + xname + tpath; ");
            sb.Append("    }  ");
            sb.Append("    if(tpath.length > 0) ");
            sb.Append("      path.push(tpath); ");
            sb.Append("  } ");
            sb.Append("} ");
            sb.Append("return path; ");
            sb.Append("} ");
            sb.Append("}); ");

            return sb.ToString();
        }

    }
}
