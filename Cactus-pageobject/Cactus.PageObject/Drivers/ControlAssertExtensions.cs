using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using Cactus.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Should;

namespace Cactus.Drivers
{
    public static class ControlAssertExtensions
    {
        /// <summary>
        /// Asserts the class string for the element is expected
        /// StringAssert.Contains controlClass, controlAssert.Control.ClassList
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlClass"></param>
        /// <param name="errorMessage">Add to the error message</param>
        /// <returns></returns>
        public static bool ShouldContainClass(this ControlAsserts controlAssert, string controlClass, string errorMessage = "")
        {
            controlAssert.Control.WaitForElementClass(controlClass);

            StringAssert.Contains(controlClass, controlAssert.Control.ClassList, "FAIL: " +
                string.Format("{1} was expecting class : {0} {2} {0} and was :{3}{0}{4}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlClass, controlAssert.Control.ClassList, errorMessage));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting class : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlClass, controlAssert.Control.ClassList));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Using GetAttribute("innerHTML");, returns the innerHTML of an element.
        /// StringAssert.Contains expectedText, controlAssert.Control.InnerHtml
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <returns></returns>
        public static bool ShouldContainInnerHtml(this ControlAsserts controlAssert, string expectedText)
        {
            StringAssert.Contains(expectedText, controlAssert.Control.InnerHtml, "FAIL: " +
                string.Format("{1} was expecting InnerHtml Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.InnerHtml));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting InnerHtml Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.InnerHtml));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }


        /// <summary>
        /// Asserts the value of the HREF in the html tag of the element. 
        /// StringAssert.Contains expectedHref, controlAssert.Control.Href
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedHref"></param>
        /// <returns></returns>
        public static bool ShouldContainHref(this ControlAsserts controlAssert, string expectedHref)
        {
            StringAssert.Contains(expectedHref, controlAssert.Control.Href, "FAIL: " +
                string.Format("{1} was expecting HREF: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedHref, controlAssert.Control.Href));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting HREF: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedHref, controlAssert.Control.Href));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText of this element, without any leading or trailing whitespace, and with other whitespace collapsed.
        /// StringAssert.Contains expectedText, controlAssert.Control.Text
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <param name="refreshPageUntilTextIsVisible"></param>
        /// <returns></returns>
        public static bool ShouldContainText(this ControlAsserts controlAssert, string expectedText, bool refreshPageUntilTextIsVisible = false)
        {
            if (refreshPageUntilTextIsVisible)
            {
                // if text is not here keep trying for x seconds.
                Stopwatch s = new Stopwatch();
                s.Start();
                while (s.Elapsed < TimeSpan.FromSeconds(20000)) //20 seconds
                {
                    if (controlAssert.Control.Text.ToLower().Contains(expectedText.ToLower()))
                        break;
                    Engine.RefreshPage();
                    Thread.Sleep(2000);
                }
                s.Stop();
            }
            else
                controlAssert.Control.WaitForElementText(expectedText);

            StringAssert.Contains(expectedText, controlAssert.Control.Text, "FAIL: " +
                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText of this element, with the Regex MatchPatern, etc.
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <param name="regexMatchPatern">@"^((([\w]+\.[\w]+)+)|([\w]+))@(([\w]+\.)+)([A-Za-z]{1,3})$";</param>
        /// <param name="fieldFromRegex"></param>
        /// <param name="isMatch"></param>
        /// <returns></returns>
        public static bool ShouldContainTextWithRegex(this ControlAsserts controlAssert, string expectedText, string regexMatchPatern, string fieldFromRegex = "", bool isMatch = false)
        {
            var reg = new Regex(regexMatchPatern);
            string str = "";
            bool match = false;
            if (!string.IsNullOrEmpty(fieldFromRegex))
                str = reg.Match(controlAssert.Control.Text).Groups[fieldFromRegex].Value;
            else if(isMatch)
            {
                // isEmail : string reg = @"^((([\w]+\.[\w]+)+)|([\w]+))@(([\w]+\.)+)([A-Za-z]{1,3})$";
                match = Regex.IsMatch(controlAssert.Control.Text, regexMatchPatern);
            }
            else
            {
                str = reg.Replace(controlAssert.Control.Text, "");
            }

            if (isMatch)
            {
                Assert.IsTrue(match, "FAIL: " +
                        string.Format("{1} was using regex to look for text: {0} {2} {0} and was :{3}", Environment.NewLine,
                        controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));
                new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was using regex to look for text: {0} {2} {0} and was :{3}", Environment.NewLine,
                        controlAssert.Control.MyBy, expectedText, str));
            }
            else
            {
                StringAssert.Contains(expectedText, str, "FAIL: " +
                    string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));
                new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, str));
            }

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Text Value of the rich Text Editor (redactor) ; 
        /// StringAssert.Contains expectedValue, controlAssert.Control.RichTextEditorValue
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedValue"></param>
        /// <returns></returns>
        public static bool ShouldContainRichTextEditorValue(this ControlAsserts controlAssert, string expectedValue)
        {
            StringAssert.Contains(expectedValue, controlAssert.Control.RichTextEditorValue, "FAIL: " +
                string.Format("{1} was expecting RichText Editor Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.RichTextEditorValue));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting RichText Editor Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.RichTextEditorValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the value of the SRC in the html tag of the element. 
        /// StringAssert.Contains expectedSrc, controlAssert.Control.Src
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedSrc"></param>
        /// <returns></returns>
        public static bool ShouldContainSrc(this ControlAsserts controlAssert, string expectedSrc)
        {
            StringAssert.Contains(expectedSrc, controlAssert.Control.Src, "FAIL: " +
                string.Format("{1} was expecting src: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedSrc, controlAssert.Control.Src));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting src: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedSrc, controlAssert.Control.Src));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Converts the grid/table in the Control to be a datatable, then Asserts the count of the dataTable object. 
        /// Assert.AreEqual expectedRowCount, datatable.Rows.Count
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedRowCount"></param>
        /// <returns></returns>
        public static bool DataTableRowCountEquals(this ControlAsserts controlAssert, int expectedRowCount)
        {
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("DataTable creation for " + controlAssert.Control.MyBy, ts)))
            {
                var dt = controlAssert.Control.DataTable();
                if (dt == null)
                    Assert.Fail(controlAssert.Control.MyBy + " |Exception| DataTable was not generated for this Control");

                Assert.AreEqual(expectedRowCount, dt.Rows.Count, "FAIL: " +
                                                                 string.Format(
                                                                     "{1} did not have the right number of Table rows: {0}Expected: {2} {0} and was :{3}",
                                                                     Environment.NewLine,
                                                                     controlAssert.Control.MyBy, expectedRowCount,
                                                                     dt.Rows.Count));

                new UxTestingLogger().LogInfo("PASS: " +
                                              string.Format(
                                                  "{1} did not have the right number of Table rows: {0}Expected: {2} {0} and was :{3}",
                                                  Environment.NewLine,
                                                  controlAssert.Control.MyBy, expectedRowCount, dt.Rows.Count));
                new TestLineStatusWithEvent().Status(TestStatus.Passed);
                return true;
            }
        }

        /// <summary>
        /// Converts the grid/table in the Control to be a datatable, then Asserts the text of the dataTable object. 
        /// You can specify row and Field name also in your Assert. 
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <param name="rowId">0 based</param>
        /// <param name="fieldName">This would be in the column header of the table.</param>
        /// <returns></returns>
        public static bool DataTableContainsText(this ControlAsserts controlAssert, string expectedText, int? rowId = null, string fieldName = null)
        {
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("DataTable creation for " + controlAssert.Control.MyBy, ts)))
            {
                var dt = controlAssert.Control.DataTable();
                if (dt == null)
                    Assert.Fail(controlAssert.Control.MyBy + " |Exception| DataTable was not generated for this Control");
                if (rowId == null && fieldName == null)
                {
                    Assert.IsTrue(
                        dt.Rows.Cast<DataRow>().Any(r => r.ItemArray.Any(c => c.ToString().Contains(expectedText))),
                        "FAIL: " + "Table did not include the text: " + expectedText);
                }
                else if (fieldName == null)
                {
                    Assert.IsTrue(dt.Rows[(int) rowId].ItemArray.Any(c => c.ToString().Contains(expectedText)),
                        "FAIL: " + "Table did not include the text: " + expectedText);
                }
                else if (rowId == null)
                {
                    Assert.IsTrue(dt.Rows.Cast<DataRow>().Any(r => r.Field<string>(fieldName).Contains(expectedText)),
                        "FAIL: " + "Table did not include the text: " + expectedText);
                }
                else
                {
                    var firstRow = dt.Rows[(int) rowId][fieldName].ToString();
                    if (!firstRow.Any())
                    {
                        Assert.Fail(controlAssert.Control.MyBy +
                                    " |Exception| Data was not available in this Row/Column");
                    }
                    StringAssert.Contains(expectedText, firstRow, "FAIL: " +
                                                                  string.Format(
                                                                      "{1} was expecting Table-Row Value: {0} {2} {0} and was :{3}",
                                                                      Environment.NewLine,
                                                                      controlAssert.Control.MyBy, expectedText, firstRow));
                }

                new UxTestingLogger().LogInfo("PASS: " + expectedText + " was found in grid/table");
                new TestLineStatusWithEvent().Status(TestStatus.Passed);
            }
            return true;
        }

        /// <summary>
        /// Asserts the Count of the _elements found for a given Control.
        /// Assert.AreEqual(expectedCount, Convert.ToInt32(controlAssert.Control.Count)
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedCount"></param>
        /// <returns></returns>
        public static bool ShouldHaveCount(this ControlAsserts controlAssert, int expectedCount)
        {
            controlAssert.Control.WaitForElementVisible();
            Assert.AreEqual(expectedCount, controlAssert.Control.Count, "FAIL: " +
                string.Format("{1} was expecting Count: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedCount, controlAssert.Control.Count));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting Count: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedCount, controlAssert.Control.Count));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the class(string) is not part of the ClassList.
        /// StringAssert.DoesNotContain controlClass, controlAssert.Control.ClassList
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlClass"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool ShouldNotContainClass(this ControlAsserts controlAssert, string controlClass, string errorMessage = "")
        {

            Support.WaitForPageReadyState();

            StringAssert.DoesNotContain(controlClass, controlAssert.Control.ClassList, "FAIL: " +
                string.Format("{1} was not expecting class : {0} {2} {0} and was :{3} {0} {4}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlClass, controlAssert.Control.ClassList, errorMessage));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was not expecting class : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlClass, controlAssert.Control.ClassList));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the expected Text is not part of the GetAttribute("innerHTML");
        /// StringAssert.DoesNotContain expectedText, controlAssert.Control.InnerHtml
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ShouldNotContainInnerHtml(this ControlAsserts controlAssert, string text)
        {
            StringAssert.DoesNotContain(text, controlAssert.Control.InnerHtml, "FAIL: " +
                string.Format("{1} was not expecting InnerHtml Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.InnerHtml));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was not expecting InnerHtml Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.InnerHtml));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the Control does not contain a text value.
        /// StringAssert.DoesNotContain expectedText, controlAssert.Control.Text
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ShouldNotContainText(this ControlAsserts controlAssert, string text)
        {
            controlAssert.Control.WaitForElementToNotHaveText();
            StringAssert.DoesNotContain(text, controlAssert.Control.Text, "FAIL: " +
                string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.Text));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.Text));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText digitsOnly of this element is same as expected,  uses regex to get only Digits from the text.
        /// StringAssert.AreNotEqualIgnoringCase digits, controlAssert.Control.TextDigitsOnly
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static bool ShouldNotEqualTextDigitsOnly(this ControlAsserts controlAssert, string digits)
        {
            StringAssert.AreNotEqualIgnoringCase(digits, controlAssert.Control.TextDigitsOnly, "FAIL: " +
                string.Format("{1} was not expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, digits, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, digits, controlAssert.Control.TextDigitsOnly));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText digitsOnly of this element is same as expected,  uses regex to get only Digits from the text.
        /// Assert.AreNotEqual(digits, Convert.ToInt32(controlAssert.Control.TextDigitsOnly)
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public static bool ShouldNotEqualTextDigitsOnly(this ControlAsserts controlAssert, int digits)
        {
            Assert.AreNotEqual(digits, Convert.ToInt32(controlAssert.Control.TextDigitsOnly), "FAIL: " +
                string.Format("{1} was not expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, digits, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, digits, controlAssert.Control.TextDigitsOnly));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts Text not equal to the innerText of this element, without any leading or trailing whitespace, and with other whitespace collapsed.
        /// StringAssert.AreNotEqualIgnoringCase text, controlAssert.Control.Text
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool ShouldNotEqualText(this ControlAsserts controlAssert, string text)
        {
            controlAssert.Control.WaitForElementToNotHaveText();
            StringAssert.AreNotEqualIgnoringCase(text, controlAssert.Control.Text, "FAIL: " +
                string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.Text));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.Text));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the HTML attribute "Value" of the element is not equal to Value inputed
        /// StringAssert.AreNotEqualIgnoringCase value, controlAssert.Control.Value
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ShouldNotEqualValue(this ControlAsserts controlAssert, string value)
        {
            StringAssert.AreNotEqualIgnoringCase(value, controlAssert.Control.Value, "FAIL: " +
                string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.Text));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.Value));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control itself does not exist on the page
        /// Assert.IsFalse controlAssert.Control.IsValidOnPage
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool ShouldNotExist(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsValidOnPage, "FAIL: " + controlAssert.Control.MyBy + " does exist on page");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " exists not on page");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText digitsOnly of this element is same as expected,  uses regex to get only Digits from the text.
        /// StringAssert.AreEqualIgnoringCase expectedDigits, controlAssert.Control.TextDigitsOnly
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedDigits"></param>
        /// <returns></returns>
        public static bool ShouldEqualTextDigitsOnly(this ControlAsserts controlAssert, string expectedDigits)
        {
            StringAssert.AreEqualIgnoringCase(expectedDigits, controlAssert.Control.TextDigitsOnly, "FAIL: " +
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText digitsOnly of this element is same as expected,  uses regex to get only Digits from the text.
        /// Assert.AreEqual(expectedDigits, Convert.ToInt32(controlAssert.Control.TextDigitsOnly)
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedDigits"></param>
        /// <returns></returns>
        public static bool ShouldEqualTextDigitsOnly(this ControlAsserts controlAssert, int expectedDigits)
        {
            controlAssert.Control.WaitForElementText(expectedDigits.ToString());

            Assert.AreEqual(expectedDigits, Convert.ToInt32(controlAssert.Control.TextDigitsOnly), "FAIL: " +
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText digitsOnly of this element is greater as expected,  uses regex to get only Digits from the text.
        /// Assert.GreaterOrEqual(Convert.ToInt32(controlAssert.Control.TextDigitsOnly), expectedDigits)
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedDigits"></param>
        /// <returns></returns>
        public static bool ShouldBeGreaterThanTextDigitsOnly(this ControlAsserts controlAssert, int expectedDigits)
        {
            controlAssert.Control.WaitForElementText(expectedDigits.ToString());

            Assert.GreaterOrEqual(Convert.ToInt32(controlAssert.Control.TextDigitsOnly), expectedDigits, "FAIL: " +
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting Digits: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedDigits, controlAssert.Control.TextDigitsOnly));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Location of the element is located withing normal tolerances.  
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="topX"></param>
        /// <param name="topY"></param>
        /// <param name="bottomX"></param>
        /// <param name="bottomY"></param>
        /// <returns></returns>
        public static bool ShouldBeLocatedBetween(this ControlAsserts controlAssert, int topX, int topY, int bottomX, int bottomY)
        {
            controlAssert.Control.WaitForElementVisible();

            Assert.IsTrue(controlAssert.Control.IsElementInCorrectLocation(topX,topY,bottomX,bottomY), "FAIL: " +
                string.Format("{1} was expecting a different Location: {0} ", Environment.NewLine,
                    controlAssert.Control.MyBy));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting Location: {0} {2} {0} and was :{2}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlAssert.Control.Location));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }


        /// <summary>
        /// Asserts the html element attribute "SRC" of this element is same as expected
        /// StringAssert.AreEqualIgnoringCase expectedSrc, controlAssert.Control.Src
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedSrc"></param>
        /// <returns></returns>
        public static bool ShouldEqualSrc(this ControlAsserts controlAssert, string expectedSrc)
        {
            StringAssert.AreEqualIgnoringCase(expectedSrc, controlAssert.Control.Src, "FAIL: " +
                string.Format("{1} was expecting src: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedSrc, controlAssert.Control.Src));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting src: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedSrc, controlAssert.Control.Src));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the innerText of this element is equal to expected, without any leading or trailing whitespace, and with other whitespace collapsed.
        /// StringAssert.AreEqualIgnoringCase expectedText, controlAssert.Control.Text
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool ShouldEqualText(this ControlAsserts controlAssert, string expectedText, bool ignoreCase = true)
        {
            controlAssert.Control.WaitForElementText(expectedText);

            if (ignoreCase)
            {
                StringAssert.AreEqualIgnoringCase(expectedText, controlAssert.Control.Text, "FAIL: " +
                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));}
            else
            {
                if (controlAssert.Control.Text != expectedText)
                {
                    Assert.Fail("FAIL: " +
                                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));
                }
            }

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the html element attribute "Value" of this element is same as expected
        /// StringAssert.AreEqualIgnoringCase expectedText, controlAssert.Control.Value
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedText"></param>
        /// <returns></returns>
        public static bool ShouldEqualValue(this ControlAsserts controlAssert, string expectedText)
        {
            StringAssert.AreEqualIgnoringCase(expectedText, controlAssert.Control.Value, "FAIL: " +
                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Text));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.Value));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control itself exists on the page.
        /// Assert.IsTrue controlAssert.Control.IsValidOnPage
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool ShouldExist(this ControlAsserts controlAssert)
        {
            controlAssert.Control.WaitForElementPresent();
            Assert.IsTrue(controlAssert.Control.IsValidOnPage, "FAIL: " + controlAssert.Control.MyBy + " does not exist on page");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " exists on page");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the class string for the element is expected
        /// StringAssert.Contains controlClass, controlAssert.Control.ClassList
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlClass"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool HasClass(this ControlAsserts controlAssert, string controlClass)
        {
            return ShouldContainClass(controlAssert, controlClass);
        }

        /// <summary>
        /// Asserts the class string for the element is expected
        /// StringAssert.Contains controlClass, controlAssert.Control.ClassList
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlClass"></param>
        /// <param name="errorMessage"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool HasClass(this ControlAsserts controlAssert, string controlClass, string errorMessage)
        {
            return ShouldContainClass(controlAssert, controlClass, errorMessage);
        }
        
        /// <summary>
        /// Given a Select Control, this should find if an option is available.
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedOptionText"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        public static bool HasOption(this ControlAsserts controlAssert, string expectedOptionText, string errorMessage = "")
        {
            controlAssert.Control.WaitForElementPresent();
            var pass = false;
            var select = new SelectElement(controlAssert.Control.Element);
            IList<IWebElement> options = select.Options; // this select.Options pulls all the options and holds them while the foreach below iterates through the list and outputs to the console
            foreach (IWebElement option in options)
            {
                if (option.Text.Equals(expectedOptionText))
                    pass = true;
            }

            Assert.IsTrue(pass, "FAIL: " +
                string.Format("{1} was expecting option : {0} {2} {0} and was :{3}{0}{4}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedOptionText, options.ToString(), errorMessage));

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting class : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedOptionText, options.ToString()));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is a Button.
        /// Assert.IsTrue controlAssert.Control.IsButton
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsButton(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsButton, "FAIL: " + controlAssert.Control.MyBy + " was not a button");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is a button");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control isChecked true
        /// Assert.IsFalse controlAssert.Control.IsChecked
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsChecked(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsChecked, "FAIL: " + controlAssert.Control.MyBy + " was not checked");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is checked");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is of type Editable 
        /// Assert.IsTrue controlAssert.Control.IsEditable  
        /// GetAttribute("class").Contains("editable");
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsEditable(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsEditable, "FAIL: " + controlAssert.Control.MyBy + " was not an editable control");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is an editable control");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is enabled
        /// Assert.IsFalse controlAssert.Control.IsDisabled
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsEnabled(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsDisabled, "FAIL: " + controlAssert.Control.MyBy + " was enabled");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is enabled");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// 
        /// StringAssert.AreEqualIgnoringCase controlId, controlAssert.Control.Id
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlId"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IdEquals(this ControlAsserts controlAssert, string controlId)
        {
            StringAssert.AreEqualIgnoringCase(controlId, controlAssert.Control.Id, "FAIL: " +
                string.Format("{1} was expecting Control ID : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlId, controlAssert.Control.Id));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Control ID : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlId, controlAssert.Control.Id));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is Disabled
        /// Assert.IsTrue controlAssert.Control.IsDisabled
        ///         return !_element.Enabled
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsDisabled(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsDisabled, "FAIL: " + controlAssert.Control.MyBy + " was not in Disabled state");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is in Disabled state");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is displayed to the UI
        /// Assert.IsTrue controlAssert.Control.IsDisplayed
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsDisplayed(this ControlAsserts controlAssert)
        {
            controlAssert.Control.WaitForElementPresent();
            Assert.IsTrue(controlAssert.Control.IsDisplayed, "FAIL: " + controlAssert.Control.MyBy + " was not displayed");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is displayed");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is not visible in the UI
        /// Assert.IsFalse controlAssert.Control.IsVisible
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsHidden(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsVisible, "FAIL: " + controlAssert.Control.MyBy + " was visible");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not visible");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is not a button
        /// Assert.IsTrue controlAssert.Control.IsButton
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotAButton(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsButton, "FAIL: " + controlAssert.Control.MyBy + " was a button");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not a button");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control isChecked false (not checked)
        /// Assert.IsFalse controlAssert.Control.IsChecked
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotChecked(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsChecked, "FAIL: " + controlAssert.Control.MyBy + " was checked");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is checked");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is not Disabled.
        /// Assert.IsFalse controlAssert.Control.IsDisabled
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotDisabled(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsDisabled, "FAIL: " + controlAssert.Control.MyBy + " was in Disabled state");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not in Disabled state");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is not of type editable.
        /// Assert.IsFalse controlAssert.Control.IsEditable
        /// GetAttribute("class").Contains("editable");
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotEditable(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsEditable, "FAIL: " + controlAssert.Control.MyBy + " was an editable control");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not an editable control");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is not Null (not found, error, etc)
        /// Assert.IsFalse controlAssert.Control.IsNull
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotNull(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsNull, "FAIL: " + controlAssert.Control.MyBy + " was null");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not null");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is not visible.
        /// Assert.IsFalse controlAssert.Control.IsElementVisible
        /// 5 step process.
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNotVisible(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsElementVisible, "FAIL: " + controlAssert.Control.MyBy + " was visible");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not visible");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that the control is null.  Not there, error, etc.
        /// Assert.IsTrue controlAssert.Control.IsNull
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool IsNull(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsNull, "FAIL: " + controlAssert.Control.MyBy + " was not null");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is null");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is a RichtextEditor type, redactor, etc.
        /// Assert.IsTrue controlAssert.Control.IsRichTextEditor
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsRichTextEditor(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsRichTextEditor, "FAIL: " +
                controlAssert.Control.MyBy + " was not a RichTextbox (redactor)");

            new UxTestingLogger().LogInfo("PASS: " + 
                controlAssert.Control.MyBy + " is a RichTextbox (redactor)");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is not a RichtextEditor type, redactor, etc.
        /// Assert.IsFalse controlAssert.Control.IsRichTextEditor
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsNotRichTextEditor(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsRichTextEditor, "FAIL: " +
                controlAssert.Control.MyBy + " was a RichTextbox (redactor)");

            new UxTestingLogger().LogInfo("PASS: " +
                controlAssert.Control.MyBy + " is not a RichTextbox (redactor)");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is Selected  (option, or UL)
        /// Assert.IsTrue controlAssert.Control.IsSelected
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsSelected(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsSelected, "FAIL: " + controlAssert.Control.MyBy + " was not selected");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is selected");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts The element has been found on the page in any kind of visibility condition.
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsPresent(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsValidOnPage, "FAIL: " + controlAssert.Control.MyBy + " was not present on page");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is present on page");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts The element has not been found on the page in any kind of visibility condition.
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsNotPresent(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsValidOnPage, "FAIL: " + controlAssert.Control.MyBy + " was present on page");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not present on page");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is not Selected  (option, or UL)
        /// Assert.IsFalse controlAssert.Control.IsSelected
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsNotSelected(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsSelected, "FAIL: " + controlAssert.Control.MyBy + " was selected");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not selected");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is a textbox type.
        /// Assert.IsTrue controlAssert.Control.IsTextBox
        /// (_element.TagName == "input" && _element.GetAttribute("type") == "text")
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsTextBox(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsTextBox, "FAIL: " + controlAssert.Control.MyBy + " was not a Textbox (input)");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is a Textbox (input)");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is not a textbox type.
        /// Assert.IsFalse controlAssert.Control.IsTextBox
        /// (_element.TagName == "input" && _element.GetAttribute("type") == "text")
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsNotTextBox(this ControlAsserts controlAssert)
        {
            Assert.IsFalse(controlAssert.Control.IsTextBox, "FAIL: " + controlAssert.Control.MyBy + " was a Textbox (input)");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is not a Textbox (input)");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is an UBERdropdown
        /// Assert.IsTrue controlAssert.Control.IsUberDropdown
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsUberDropdown(this ControlAsserts controlAssert)
        {
            Assert.IsTrue(controlAssert.Control.IsUberDropdown, "FAIL: " +
                controlAssert.Control.MyBy + " was not an UberDropdown (special selector)");

            new UxTestingLogger().LogInfo("PASS: " + 
                controlAssert.Control.MyBy + " is an UberDropdown (special selector)");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control is not visible.
        /// Assert.IsTrue controlAssert.Control.IsElementVisible
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <returns></returns>
        public static bool IsVisible(this ControlAsserts controlAssert)
        {
            controlAssert.Control.WaitForElementVisible();

            Assert.IsTrue(controlAssert.Control.IsElementVisible, "FAIL: " + controlAssert.Control.MyBy + " was not visible");

            new UxTestingLogger().LogInfo("PASS: " + controlAssert.Control.MyBy + " is visible");
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the OPTION values from a SELECT tag type Control are present.
        /// </summary>
        /// <param name="controlAssert">This should be a SELECT tag type Control</param>
        /// <param name="expectedText">string[]</param>
        /// <returns></returns>
        public static bool ListShouldContainOption(this ControlAsserts controlAssert, string[] expectedText)
        {
            if (controlAssert.Control.SelectElement == null)
                Assert.Fail("FAIL: Control Element given was not a Select HTML element " + controlAssert.Control.MyBy);

            var options = controlAssert.Control.SelectElement.Options;
            var listFromTest = options.Select(optionElement => optionElement.Text).ToList();

            listFromTest.CompareLists(expectedText);

            var stringlistFromTest = listFromTest.Aggregate("", (current, item) => current + "," + item).Remove(0, 1);
            var expectedlistFromTest = expectedText.Aggregate("", (current, item) => current + "," + item).Remove(0, 1);
            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedlistFromTest, stringlistFromTest));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the OPTION values from a SELECT tag type Control are present.
        /// </summary>
        /// <param name="controlAssert">This should be a SELECT tag type Control</param>
        /// <param name="expectedText"></param>
        /// <returns></returns>
        public static bool ListShouldContainOption(this ControlAsserts controlAssert, string expectedText)
        {
            if (controlAssert.Control.SelectElement == null)
                Assert.Fail("FAIL: Control Element given was not a Select HTML element " + controlAssert.Control.MyBy);

            var options = controlAssert.Control.SelectElement.Options;
            var listFromTest = options.Select(optionElement => optionElement.Text).ToList();

            var stringlistFromTest = listFromTest.Aggregate("", (current, item) => current + "," + item).Remove(0, 1);

            if (expectedText.Contains(","))
            {
                // make this a list, and search all in the list.
                var listToTestOn = expectedText.Replace(" ,", ",").Split(',');
                listFromTest.CompareLists(listToTestOn);
            }
            else
            {
                StringAssert.Contains(expectedText, stringlistFromTest, "FAIL: " +
                    string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, stringlistFromTest));
            }

            new UxTestingLogger().LogInfo("PASS: " + string.Format("{1} was expecting text: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, stringlistFromTest));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }


        /// <summary>
        /// Asserts the controls HTML attribute of Name is the expected Name
        /// StringAssert.AreEqualIgnoringCase controlName, controlAssert.Control.Name
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        public static bool NameEquals(this ControlAsserts controlAssert, string controlName)
        {
            StringAssert.AreEqualIgnoringCase(controlName, controlAssert.Control.Name, "FAIL: " +
                string.Format("{1} was expecting Control Name : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlName, controlAssert.Control.Name));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Control Name : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlName, controlAssert.Control.Name));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the controls HTML attribute of Name is not the inputed Name
        /// StringAssert.AreNotEqualIgnoringCase controlName, controlAssert.Control.Name
        /// </summary>
        /// <param name="controlAssert">Any HTML element that has a @name</param>
        /// <param name="controlName"></param>
        /// <returns></returns>
        public static bool NameDoesNotEquals(this ControlAsserts controlAssert, string controlName)
        {
            StringAssert.AreNotEqualIgnoringCase(controlName, controlAssert.Control.Name, "FAIL: " +
                string.Format("{1} was not expecting Control Name : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlName, controlAssert.Control.Name));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Control Name : {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, controlName, controlAssert.Control.Name));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the MaxLength html attribute to the expected value.
        /// Assert.AreEqual(expectedDigits, Convert.ToInt32(controlAssert.Control.TextDigitsOnly)
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="expectedMaxLength"></param>
        /// <returns></returns>
        public static bool ShouldEqualMaxLength(this ControlAsserts controlAssert, int expectedMaxLength)
        {
            Assert.AreEqual(expectedMaxLength, Convert.ToInt32(controlAssert.Control.MaxLength), "FAIL: " +
                string.Format("{1} was expecting MaxLength: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedMaxLength, controlAssert.Control.TextDigitsOnly));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting MaxLength: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedMaxLength, controlAssert.Control.MaxLength));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Displayed Text is accurate for the SELECT or UL control that is given.
        /// StringAssert.Contains expectedText, controlAssert.Control.SelectedTextDisplayed
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="expectedText"></param>
        /// <returns></returns>
        public static bool SelectedTextDisplayed(this ControlAsserts controlAssert, string expectedText)
        {
            Support.WaitForPageReadyState();
            StringAssert.Contains(expectedText, controlAssert.Control.SelectedTextDisplayed, "FAIL: " +
                string.Format("{1} was expecting Selected Text Displayed: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.SelectedTextDisplayed));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Selected Text Displayed: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedText, controlAssert.Control.SelectedTextDisplayed));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Displayed Text is not valid for the SELECT or UL control that is given.
        /// StringAssert.DoesNotContain text, controlAssert.Control.SelectedTextDisplayed
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool SelectedTextIsNotDisplayed(this ControlAsserts controlAssert, string text)
        {
            StringAssert.DoesNotContain(text, controlAssert.Control.SelectedTextDisplayed, "FAIL: " +
                string.Format("{1} was not expecting Selected Text Displayed: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.SelectedTextDisplayed));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Selected Text Displayed: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, text, controlAssert.Control.SelectedTextDisplayed));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Selected VALUE is valid for the SELECT or UL control that is given.
        /// StringAssert.Contains expectedValue, controlAssert.Control.SelectedValue
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="expectedValue"></param>
        /// <returns></returns>
        public static bool SelectedValueContains(this ControlAsserts controlAssert, string expectedValue)
        {
            StringAssert.Contains(expectedValue, controlAssert.Control.SelectedValue, "FAIL: " +
                string.Format("{1} was expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.SelectedValue));

            new UxTestingLogger().LogInfo("PASS: " + 
                string.Format("{1} was expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.SelectedValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Selected VALUE does not contain text for the SELECT or UL control that is given.
        /// StringAssert.DoesNotContain value, controlAssert.Control.SelectedValue
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SelectedIsNotValueContains(this ControlAsserts controlAssert, string value)
        {
            StringAssert.DoesNotContain(value, controlAssert.Control.SelectedValue, "FAIL: " +
                string.Format("{1} was not expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.SelectedValue));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.SelectedValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts the Selected VALUE is equal to the expectedValue for the SELECT or UL control that is given.
        /// StringAssert.AreEqualIgnoringCase expectedValue, controlAssert.Control.SelectedValue
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="expectedValue"></param>
        /// <returns></returns>
        public static bool SelectedValueEquals(this ControlAsserts controlAssert, string expectedValue)
        {
            StringAssert.AreEqualIgnoringCase(expectedValue, controlAssert.Control.SelectedValue, "FAIL: " +
                string.Format("{1} was expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.SelectedValue));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, expectedValue, controlAssert.Control.SelectedValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// 
        /// StringAssert.AreNotEqualIgnoringCase value, controlAssert.Control.SelectedValue
        /// </summary>
        /// <param name="controlAssert">SELECT OR UL control element</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SelectedIsNotValueEquals(this ControlAsserts controlAssert, string value)
        {
            StringAssert.AreNotEqualIgnoringCase(value, controlAssert.Control.SelectedValue, "FAIL: " +
                string.Format("{1} was not expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.SelectedValue));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} was not expecting Selected Value: {0} {2} {0} and was :{3}", Environment.NewLine,
                    controlAssert.Control.MyBy, value, controlAssert.Control.SelectedValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// Asserts that this control has an attribute with the specified name and value
        /// Assert.IsFalse controlAssert.Control.IsDisabled
        /// </summary>
        /// <param name="controlAssert"></param>
        /// <param name="attributeName"></param>
        /// <param name="expectedValue"></param>
        /// <returns>true/false. Can be used in If statements</returns>
        public static bool HasAttributeValue(this ControlAsserts controlAssert, string attributeName, string expectedValue)
        {
            var actualValue = controlAssert.Control.GetAttributeValue(attributeName);

            StringAssert.AreEqualIgnoringCase(actualValue, expectedValue, "FAIL: " +
                string.Format("{1} does not have attribute {0}{2}{0} or the attribute value did not equal: {0}{3}{0} and was: {4}", Environment.NewLine,
                    controlAssert.Control.MyBy, attributeName, expectedValue, actualValue));

            new UxTestingLogger().LogInfo("PASS: " +
                string.Format("{1} has attribute {0}{2}{0} and the expected value: {0}{3}{0} matched the actual: {4}", Environment.NewLine,
                    controlAssert.Control.MyBy, attributeName, expectedValue, actualValue));
            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return true;
        }

        /// <summary>
        /// On deconstruction, if the test case failed during one of these methods in this class, 
        /// it will alert the Event Watchers of a failure.
        /// </summary>
#pragma warning disable 465
        static void Finalize()
#pragma warning restore 465
        {
            if (TestContext.CurrentContext.Result.Status == TestStatus.Failed)
            {
                new TestLineStatusWithEvent().Status(TestStatus.Failed);
            }
        }

        public static IEnumerable<T> ShouldHaveCount<T>(this IEnumerable<T> actual, int expected)
        {
            actual.Count().ShouldEqual(expected);
            return actual;
        }
    }

    public delegate void TestLineCompleteEventHandler(object sender, EventArgs e);
    public delegate void TestLinePassedEventHandler(object sender, EventArgs e);
    public delegate void TestLineFailedEventHandler(object sender, EventArgs e);

    public class TestLineStatusWithEvent
    {
        public event TestLineCompleteEventHandler Complete;
        public event TestLinePassedEventHandler Passed;
        public event TestLineFailedEventHandler Failed;

        protected virtual void OnComplete(EventArgs e)
        {
            if (Complete != null)
                Complete(this, e);
        }

        protected virtual void OnPassed(EventArgs e)
        {
            if (Passed != null)
                Passed(this, e);
        }

        protected virtual void OnFailed(EventArgs e)
        {
            if (Failed != null)
                Failed(this, e);
        }

        public void Status(TestStatus testLineStatus)
        {
            switch (testLineStatus)
            {
                case TestStatus.Failed:
                    OnFailed(EventArgs.Empty);
                    OnComplete(EventArgs.Empty);
                    break;
                case TestStatus.Passed:
                    OnPassed(EventArgs.Empty);
                    OnComplete(EventArgs.Empty);
                    break;
                case TestStatus.Skipped:
                    OnComplete(EventArgs.Empty);
                    break;
                case TestStatus.Inconclusive:
                    OnComplete(EventArgs.Empty);
                    break;
            }
        }
}

    /// <summary>
    /// The ControlAsserts class is so that if you have a Control, it will allow you to use the methods as extensions of a Control in:
    /// ControlAssertExtensions class.   Example:   NameControl.Assert.ShouldContainText("my name");
    /// </summary>
    public class ControlAsserts
    {
        public ControlAsserts(Control control)
        {
            if (control == null)
                throw new NoSuchElementException("No element present, ControlAsserts was passed a null value.");
            Control = control;
        }

        public Control Control { get; set; }
    }

}