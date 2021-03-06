﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Cactus.Infrastructure;
using HtmlTags;
using HtmlTags.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Interactions.Internal;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.UI;
using How = Cactus.Drivers.PageObject.How;
using Keys = OpenQA.Selenium.Keys;

namespace Cactus.Drivers
{
    /// <summary>
    /// A Control is a expanded form of an IWebElement.  Asserts and many extensions have been added.
    /// </summary>
    public class Control
    {
        #region Variables and Constants

        public static int DefaultTimeout = 2;

        ILogger _log;

        readonly WebDriverWait _wait = new WebDriverWait(Engine.WebDriver, TimeSpan.FromMinutes(DefaultTimeout));
        public OpenQA.Selenium.By MyBy;
        readonly string selector;
        readonly How how;

        public ControlAsserts Assert { get; private set; }

        public string SelectorUsed
        {
            get { return selector; }
        }

        public How HowUsed
        {
            get { return how; }
        }

        #endregion

        #region Constructors

        private Control()
        {
            _log = new UxTestingLogger();
            Assert = new ControlAsserts(this);
        }

        /// <summary>
        /// Base constructor for the Element Controller.
        /// How is "how" you want the selector to be identified.
        /// Value is only used for HTMLTag identification.
        /// The logic for this is that each Control By Selector is loaded into memory, but not used.
        /// Every Control on the PageObject pages will be pulled in at the beginning of this sample line of code.
        ////    using (var page = new PageObjectFileXX)
        ////    {
        ////    }
        /// On the end of the using, all memory will be released.  PageObject files are IDisposable.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="_selector"></param>
        /// <param name="value"></param>
        public Control(How type, string _selector, string value = null) : this()
        {
            try
            {
                selector = _selector;
                how = type;
                switch (type)
                {
                    case How.ClassName:
                        MyBy = OpenQA.Selenium.By.ClassName(_selector);
                        break;
                    case How.CssSelector:
                        MyBy = OpenQA.Selenium.By.CssSelector(_selector);
                        break;
                    case How.DataSupapicka:
                        MyBy = CustomBy.DataSupapicka(_selector);
                        break;
                    case How.Id:
                        MyBy = OpenQA.Selenium.By.Id(_selector);
                        break;
                    case How.LinkText:
                        MyBy = OpenQA.Selenium.By.LinkText(_selector);
                        break;
                    case How.Name:
                        MyBy = OpenQA.Selenium.By.Name(_selector);
                        break;
                    case How.PartialLinkText:
                        MyBy = OpenQA.Selenium.By.PartialLinkText(_selector);
                        break;
                    case How.TagName:
                        MyBy = OpenQA.Selenium.By.TagName(_selector);
                        break;
                    case How.XPath:
                        MyBy = OpenQA.Selenium.By.XPath(_selector);
                        break;
                    case How.jQuery:
                        MyBy = CustomBy.jQuery(_selector);
                        break;
                    case How.HtmlTag:
                        MyBy = CustomBy.HtmlTag(_selector, value);
                        break;
                    case How.CssValue:
                        MyBy = CustomBy.CssValue(_selector);
                        break;
                    case How.Src:
                        MyBy = CustomBy.Src(_selector);
                        break;
                    default:
                        MyBy = OpenQA.Selenium.By.Name(_selector);
                        break;
                }
            }
            catch (WebDriverException webDriverException)
            {
                _log.Exception(webDriverException);
                throw;
            }
            catch (XPathException xPathException)
            {
                _log.Exception(xPathException);
                throw;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// Add control items to an previously found IWebElement.
        /// </summary>
        /// <param name="element"></param>
        public Control(IWebElement element) : this()
        {
            try
            {
                MyBy = OpenQA.Selenium.By.XPath(Support.GenerateXpathFromElement(element));
            }
            catch
            {
                MyBy = OpenQA.Selenium.By.Name("NoBy");
            }

            _element = element;
        }

        /// <summary>
        /// Add control items to an previously found IWebElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="recordingBy">This is just incase we need it for  error message</param>
        public Control(IWebElement element, OpenQA.Selenium.By recordingBy)
            : this()
        {
            var script = xpathbuilder();
            const string command = "return getElementXPath(arguments[0]);";

            var xpath = Engine.Execute<string>(Engine.WebDriver, script + " " + command, element);
            //_log.Debug(SelectorUsed + " has xpath = " + xpath);

            if (xpath !=null)
                MyBy = By.XPath(xpath); 
            else
            {
                MyBy = recordingBy;
            }
            _element = element;
        }

        /// <summary>
        /// Add control items to an previously found IWebElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="recordingBy">This is just incase we need it for  error message</param>
        public Control(IWebElement element, By recordingBy)
            : this()
        {
            MyBy = recordingBy;
            _element = element;
        }

        /// <summary>
        /// Add control items to an previously found IWebElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="recordingBy">This is just incase we need it for  error message</param>
        public Control(IWebElement element, CustomBy recordingBy)
            : this()
        {
            MyBy = recordingBy;
            _element = element;
        }
        #endregion

        #region Control Add on Methods

        /// <summary>
        /// Validate the XPath locator string.  This will help debugging during coding of tests.
        /// </summary>
        /// <param name="xPathString"></param>
        public static void AssertValidXpath(string xPathString)
        {
            XmlDocument doc = new XmlDocument();
            XPathNavigator nav = doc.CreateNavigator();
            // will throw invalid Xpath expression (XPathException)
            XPathExpression expr = nav.Compile(xPathString);
        }

        /// <summary>
        /// Add a class from an element
        /// </summary>
        /// <param name="class"></param>
        public void AddClass(string @class)
        {
            getElement();
            Engine.Execute<object>("arguments[0].classList.add('" + @class + "');", _element);
        }

        /// <summary>
        /// Gets the Alt tag data of the Image element
        /// example: <img alt="stuff" src="/agent/_content/images/dt-logo-trans.png">  
        /// </summary>
        public string Alt
        {
            get
            {
                getElement();
                return _element.GetAttribute("alt");
            }
        }

        /*
          AssertExists (this would be send in element, fail if not there)
        */

        /// <summary>
        /// Returns the Style BorderColor for the element
        /// </summary>
        public string BorderColor
        {
            get
            {
                getElement(highlight: false);
                var result = _element.GetCssValue("borderColor");
                //var result = Engine.Execute<string>(Engine.WebDriver, "return arguments[0].style.borderColor", _element);
                return result ?? "";
            }
        }

        /// <summary>
        /// Returns the Style BorderColor for the element
        /// </summary>
        public string BorderStyle
        {
            get
            {
                getElement(highlight: false);
                var result = Engine.Execute<string>(Engine.WebDriver, "return  document.defaultView.getComputedStyle(arguments[0],null).getPropertyValue('border')", _element);
                return result ?? "";
            }
        }

        /// <summary>
        /// Returns the Style Border width for the element
        /// </summary>
        public string BorderWidth
        {
            get
            {
                getElement(highlight: false);
                var result = Engine.Execute<string>(Engine.WebDriver, "return  document.defaultView.getComputedStyle(arguments[0],null).getPropertyValue('border-width')", _element);
                return result ?? "";
            }
        }
        
        /// <summary>
        /// Returns the class string for the element
        /// </summary>
        public string ClassList
        {
            get
            {
                getElement(highlight: false);
                return _element.GetAttribute("class");
            }
        }

        /// <summary>
        ///  This is the same Clear as IWebElement.Clear, but with metrics added.
        /// </summary>
        public void Clear()
        {
            getElement();
            try
            {
                _element.Clear();
                _log.Debug(MyBy + " cleared");
            }
            catch
            {
                _log.Error(MyBy + " failed to clear");
                throw;
            }
        }

        /// <summary>
        /// Clear and Send Keys to element
        /// </summary>
        /// <param name="text"></param>
        public void ClearAndSetText(string text)
        {
            getElement(highlight: false);
            try
            {
                _element.Clear();
                _element.SendKeys(text);
            }
            catch
            {
                _log.Error(MyBy + " failed to enter text");
                throw;
            }
        }

        /// <summary>
        /// Performs a straight-foward Element.Click with logging and exception handling.
        /// </summary>
        public void ClickSimple()
        {
            try
            {
                _log.Debug("Clicking || " + MyBy);
                getElement(safeMode: true, refreshElement: true);
                _element.Click();
            }
            catch (Exception ex)
            {
                _log.Error(MyBy + " could not be clicked ", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Click the element attached to this call
        /// </summary>
        /// <param name="actionClick"></param>
        /// <param name="waitForDisappearance"></param>
        /// <param name="highlight"></param>
        public void Click(bool actionClick = true, bool waitForDisappearance = false, bool highlight = true)
        {
            var action = new Actions(Engine.WebDriver);

            Support.WaitForPageReadyState();
            getElement(highlight: highlight, safeMode: true, refreshElement: true);
            if (_element == null) // for stability of test runs
            {
                Support.WaitUntilElementIsVisible(MyBy);
                if (_element == null)
                {
                    throw new NoSuchElementException("No element to click");
                }
            }
            try
            {
                action.MoveToElement(_element).Build().Perform(); //pre Hover.  To help on VMs with viewports out of range.
                Thread.Sleep(5);

                _log.Debug("Clicking || " + MyBy);
                if (actionClick)
                {
                    
                    var readyClick = action.MoveToElement(_element).Click().Build();
                    readyClick.Perform();
                    if (waitForDisappearance)
                    {
                        try
                        {
                            Support.WaitUntilElementIsNotVisible(MyBy, TimeSpan.FromSeconds(13));
                        }
                        catch (Exception)
                        {
                           //ignore
                        }
                    }
                }
                else
                {
                    _element.Click();
                    if (waitForDisappearance)
                    {
                        using (PerformanceTimer.Start(
                            ts => PerformanceTimer.LogTimeResult("WaitUntilElementIsNotVisible " + MyBy, ts)))
                        {
                            try
                            {
                                Support.WaitUntilElementIsNotVisible(MyBy, TimeSpan.FromSeconds(12));
                            }
                            catch (Exception)
                            {
                                //ignore
                            }
                        }
                    }
                }

                // wait for page transitions
                Thread.Sleep(50);
                Support.WaitForPageReadyState(TimeSpan.FromSeconds(16));
            }
            catch (StaleElementReferenceException)
            {
                _element = Engine.WebDriver.FindElement(MyBy);
                _log.Error(MyBy + " was stale, trying WebDriver click now ");
                _element.Click();
            }
            catch (InvalidOperationException invalidOperationException)
            {
                _log.Error(MyBy + " could not be clicked ", invalidOperationException);
                Support.ScreenShot();
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(MyBy + " could not be clicked ", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Click the element attached to this call
        /// Wait 20 milliseconds for new page to start loading.
        /// </summary>
        /// <param name="actionClick"></param>
        public void ClickAndWait(bool actionClick = false)
        {
            Click(actionClick);
            Thread.Sleep(TimeSpan.FromMilliseconds(20));
            Support.WaitForPageReadyState();
        }

        /// <summary>
        /// Click the element attached to this call
        /// </summary>
        /// <param name="pauseTimeSpan">TimeSpan to Pause</param>
        /// <param name="actionClick"></param>
        public void ClickAndWait(TimeSpan pauseTimeSpan, bool actionClick = false)
        {
            Click(actionClick);
            Thread.Sleep(pauseTimeSpan);
            Support.WaitForPageReadyState();
        }

        /// <summary>
        /// Click on an element, but also use the x,y axis of that element.  
        /// This is useful for images
        /// </summary>
        /// <param name="x">Horizontal</param>
        /// <param name="y">Vertical</param>
        public void ClickAt(int x = 1, int y = 1)
        {
            getElement(refreshElement: true);
            new Actions(Engine.WebDriver)
                .MoveToElement(_element, 0, 0)
                .MoveByOffset(x, y)
                .Click()
                .Build()
                .Perform();
        }

        /// <summary>
        /// Click on an element, but also use the x,y axis of that element.  
        /// This is useful for images
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="x">Horizontal</param>
        /// <param name="y">Vertical</param>
        public static void ClickAt(By selector, int x = 1, int y = 1)
        {
            var rootElement = Engine.FindElement(selector);

            new Actions(Engine.WebDriver)
                .MoveToElement(rootElement, 0, 0)
                .MoveByOffset(x, y)
                .Click()
                .Build()
                .Perform();
        }

        /// <summary>
        /// Click on an element, but also use the x,y axis of that element.  
        /// This is useful for images
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="x">Horizontal</param>
        /// <param name="y">Vertical</param>
        public static void ClickAt(OpenQA.Selenium.By selector, int x = 1, int y = 1)
        {
            var rootElement = Engine.FindElement(selector);

            new Actions(Engine.WebDriver)
                .MoveToElement(rootElement, 0, 0)
                .MoveByOffset(x, y)
                .Click()
                .Build()
                .Perform();
        }

        /// <summary>
        /// Click on an element, but also use the x,y axis of that element.  
        /// This is useful for images
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="x">Horizontal</param>
        /// <param name="y">Vertical</param>
        public static void ClickAt(CustomBy selector, int x = 1, int y = 1)
        {
            var rootElement = Engine.FindElement(selector);

            new Actions(Engine.WebDriver)
                .MoveToElement(rootElement, 0, 0)
                .MoveByOffset(x, y)
                .Click()
                .Build()
                .Perform();
        }

        /// <summary>
        /// Click on an element, but also use the x,y axis of that element.  
        /// This is useful for images
        /// </summary>
        /// <param name="element"></param>
        /// <param name="x">Horizontal</param>
        /// <param name="y">Vertical</param>
        public static void ClickAt(IWebElement element, int x = 1, int y = 1)
        {
            new Actions(Engine.WebDriver)
                .MoveToElement(element, 0, 0)
                .MoveByOffset(x, y)
                .Click()
                .Build()
                .Perform();
        }

        /// <summary>
        /// Selenium Context Click (right click)
        /// </summary>
        public void ContextClick()
        {
            getElement(refreshElement: true);
            new Actions(Engine.WebDriver)
                .MoveToElement(_element)
                .ContextClick(_element)
                .Build()
                .Perform();
        }


        /// <summary>
        /// Using a web grid element , i.e. "gridContainer_Case" from Query Window, find the first row of data and click the first column.
        /// </summary>
        public void ClickFirstRowWithData()
        {
            getElement(); //input would be table on the UI.
            foreach (var tr in _element.FindElements(OpenQA.Selenium.By.TagName("tr")))
            {
                foreach (var td in tr.FindElements(OpenQA.Selenium.By.TagName("td")))
                {
                    if (!string.IsNullOrEmpty(td.Text))
                    {
                        var action = new Actions(Engine.WebDriver);
                        var readyClick = action.MoveToElement(td).Click().Build();
                        readyClick.Perform();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Using a web grid element , i.e. "gridContainer_Case" from Query Window, find the data in the table and click that cell.
        /// </summary>
        /// <param name="textToSearchFor"></param>
        /// <param name="doubleClick">optional doubleclick action</param>
        public void ClickRow(string textToSearchFor, bool doubleClick = false)
        {
            getElement(); //input would be table on the UI.
            foreach (var tr in _element.FindElements(OpenQA.Selenium.By.TagName("tr")))
            {
                foreach (var td in tr.FindElements(OpenQA.Selenium.By.TagName("td")))
                {
                    if (!string.IsNullOrEmpty(td.Text) && td.Text.Contains(textToSearchFor))
                    {
                        if (doubleClick)
                        {
                            var action = new Actions(Engine.WebDriver);
                            action.DoubleClick(td);
                            action.Perform();
                        }
                        else
                        {
                            var action = new Actions(Engine.WebDriver);
                            var readyClick = action.MoveToElement(td).Click().Build();
                            readyClick.Perform();
                        }
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Using a web grid element , i.e. "gridContainer_Case" from Query Window, find the data in the table and click that cell.
        /// </summary>
        /// <param name="rowNumber"></param>
        /// <param name="doubleClick">optional doubleclick action</param>
        public void ClickRow(int rowNumber, bool doubleClick = false)
        {
            getElement(); //input would be table on the UI.
            var count = 1;
            foreach (var tr in _element.FindElements(OpenQA.Selenium.By.TagName("tr")))
            {
                if (count == rowNumber)
                {
                    foreach (var td in tr.FindElements(OpenQA.Selenium.By.TagName("td")))
                    {
                        if (!string.IsNullOrEmpty(td.Text))
                        {
                            if (doubleClick)
                            {
                                var action = new Actions(Engine.WebDriver);
                                action.DoubleClick(td);
                                action.Perform();
                            }
                            else
                            {
                                var action = new Actions(Engine.WebDriver);
                                var readyClick = action.MoveToElement(td).Click().Build();
                                readyClick.Perform();
                            }
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Make the checkbox checked if the Checkbox is not checked. If displayed = false, it will still check for the value.
        /// </summary>
        public void Check()
        {
            getElement();
            if (_element.Displayed)
            {
               if (_element.Selected || _element.GetAttribute("checked") == "true" || _element.GetAttribute("checked") == "checked")
                  return;  // already checked.

               var action = new Actions(Engine.WebDriver);
               var readyClick = action.MoveToElement(_element).Click().Build();
               readyClick.Perform();
               Thread.Sleep(1000);
               return;
            }
            // may need to throw error here later.
            _log.Error(MyBy + " is not Displayed");
            return;
        }

        /// <summary>
        /// Return the computed string Style value via javascript
        /// </summary>
        public string ComputedStyle
        {
            get
            {
                getElement(highlight: false, refreshElement: true);
                var sb = new StringBuilder();
                sb.Append(" var elem = arguments[0]; ");
                sb.Append(" var compstring = document.defaultView.getComputedStyle(elem, null); ");
                sb.Append(" return compstring; ");
                var style = Engine.Execute<object>(sb.ToString(), _element);

                return JsonUtil.ToJson(style);
            }
        }

        /// <summary>
        /// Return the computed string Style value via javascript
        /// </summary>
        public string ComputedStyleVisability
        {
            get
            {
                getElement(highlight: false, refreshElement: true);
                var sb = new StringBuilder();
                sb.Append(" var elem = arguments[0]; ");
                sb.Append(" if (elem.currentStyle) { ");
                sb.Append("     var vis = elem.currentStyle['visibility']; ");
                sb.Append(" } else { ");
                sb.Append(
                    "     var vis = document.defaultView.getComputedStyle(elem, null).getPropertyValue('visibility'); ");
                sb.Append(" } ");
                sb.Append(" return vis; ");
                var style = Engine.Execute<string>(sb.ToString(), _element);
                return style;
            }
        }

        /// <summary>
        /// Returns the Coordinates of the Control
        /// </summary>
        public ICoordinates Coordinates
        {
            get
            {
                getElement();
                return ((ILocatable)_element).Coordinates;
            }
        }

        /// <summary>
        /// Returns the Count of the Elements for this Control
        /// </summary>
        public int Count
        {
            get
            {
                getElements(safeMode: true, refreshElement: true);
                if (_elements == null)
                    return 0;
                return _elements.Count;
            }
        }

        /// <summary>
        /// mcdropdown_menu UL lists have data-list-name associated with them.
        /// Returns like example: data-list-name="CaseType"
        /// </summary>
        public string DataListName
        {
            get
            {
                getElement(highlight: false);
                return _element.GetAttribute("data-list-name");
            }
        }

        /// <summary>
        /// Using a web grid element , i.e. "gridContainer_Case" from Query Window, convert to a datatable.
        /// Returns like example: data-list-name="CaseType"
        /// </summary>
        public DataTable DataTable(OpenQA.Selenium.By tableHeaderSelector = null)
        {
            var table = new DataTable();
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("DataTable creation " + selector, ts)))
            {
                Support.WaitForPageReadyState();
                //wait for the selector to have some kind of text in it.

                WaitForElementText();
                getElement(highlight: false);

                // get table headers, used in the Assert logic.
                IWebElement tableHeaderElement;
                // if tableHeaderSelector was given, add column names to this table.
                if (tableHeaderSelector == null)
                {
                    // try the default.  Note: This is not in the same table.
                    tableHeaderElement = Engine.FindElement(OpenQA.Selenium.By.ClassName("ui-jqgrid-labels"), safeMode: true);
                    if (tableHeaderElement == null)
                    {
                        try
                        {
                            tableHeaderElement = _element.FindElement(OpenQA.Selenium.By.TagName("thead"));
                        }
                        catch
                        {
                            //ignore. leave & tableHeaderElement = null
                        }
                    }
                }
                else
                {
                    tableHeaderElement = _element.FindElement(tableHeaderSelector);
                }

                // generate columns based on table headers.
                if (tableHeaderElement != null)
                {
                    var thDivHeaderCount = tableHeaderElement.FindElements(OpenQA.Selenium.By.XPath(".//th/div")).Count;
                    foreach (var thHeader in tableHeaderElement.FindElements(OpenQA.Selenium.By.TagName("th")))
                    {
                        
                        if (thDivHeaderCount > 0)
                        {
                            foreach (var tdHeader in thHeader.FindElements(OpenQA.Selenium.By.TagName("div")))
                            {
                                if (string.IsNullOrEmpty(tdHeader.Text))
                                    continue;
                                DataColumn column = new DataColumn
                                {
                                    DataType = Type.GetType("System.String"),
                                    ColumnName = tdHeader.Text.Replace(@"""", "").Trim()
                                };
                                table.Columns.Add(column);
                            }
                        }
                        else
                        {
                            if (thHeader.GetAttribute("class").Contains("default-column"))
                            {
                                DataColumn column = new DataColumn
                                {
                                    DataType = Type.GetType("System.String"),
                                    ColumnName = "default"
                                };
                                table.Columns.Add(column);
                            }
                            else if (thHeader.GetAttribute("class").Contains("drag-column"))
                            {
                                DataColumn column = new DataColumn
                                {
                                    DataType = Type.GetType("System.String"),
                                    ColumnName = "drag-able"
                                };
                                table.Columns.Add(column);
                            }
                            else if (!string.IsNullOrEmpty(thHeader.Text))
                            {
                                DataColumn column = new DataColumn
                                {
                                    DataType = Type.GetType("System.String"),
                                    ColumnName = thHeader.Text.Replace(@"""", "").Trim()
                                };
                                table.Columns.Add(column);
                            }
                            else
                            {
                                // do not add column.
                            }
                        }

                    }
                    var columnCount = _element.FindElements(OpenQA.Selenium.By.TagName("tr"));
                    if (columnCount == null || columnCount.Count < 2)
                    {
                        // wait for the rows to show up.
                        Support.WaitUntilChildElementsArePresent(_element, OpenQA.Selenium.By.TagName("tr"), countGreaterThan: 1);
                    }
                    // use preformed columns.
                    foreach (var tr in _element.FindElements(OpenQA.Selenium.By.TagName("tr")))
                    {
                        if (tr.FindElement(OpenQA.Selenium.By.XPath("..")).TagName.Equals("thead"))
                            continue;
                        if (tr.GetAttribute("id") == null || tr.GetAttribute("class").Contains("jqgfirstrow"))
                            continue;
                        DataRow row = table.NewRow();
                        var columnIndex = 0;
                        foreach (var td in tr.FindElements(OpenQA.Selenium.By.TagName("td")))
                        {
                            if (!td.Displayed)
                                continue;
                            if (!string.IsNullOrEmpty(td.Text))
                            {
                                row[columnIndex] = td.Text;
                            }
                            else if (td.FindElements(By.TagName("input")).Count == 1)
                            {
                                row[columnIndex] = td.FindElement(By.TagName("input")).Text;
                            }
                            columnIndex++;
                        }
                        table.Rows.Add(row);
                    }
                }
                    // default to using no table headers.  
                else
                {
                    var columnCount = _element.FindElements(OpenQA.Selenium.By.XPath(".//tr[0]/td"));
                    if (columnCount == null)
                        return null;
                    for (int i = 0; i < columnCount.Count; i++)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute
                        table.Columns.Add(i.ToString(), type: Type.GetType("System.String"));
                    }
                    foreach (var tr in _element.FindElements(OpenQA.Selenium.By.TagName("tr")))
                    {
                        DataRow row = table.NewRow();
                        var columnIndex = 0;
                        foreach (var td in  tr.FindElements(OpenQA.Selenium.By.TagName("td")))
                        {
                            if (!string.IsNullOrEmpty(td.Text))
                            {
                                row[columnIndex] = td.Text;
                            }
                            columnIndex++;
                        }
                        table.Rows.Add(row);
                    }
                }
            }
            return table;
        }

        /// <summary>
        /// Get the data-validateurl of an A tag.
        /// example: <a type="button" name="search-criteria-excel" data-validateurl="/agent/_validate/case" href="/agent/_excel/case" class="ui-button"><span>Export to excel</span></a>
        /// </summary>
        public string DataValidateUrl
        {
            get
            {
                getElement();
                return _element.GetAttribute("data-validateurl");
            }
        }

        /// <summary>
        /// Return the text Direction attribute:  DIR
        /// dir="ltr"
        /// </summary>
        public string Dir
        {
            get
            {
                getElement(highlight: false);
                if (string.IsNullOrEmpty(_element.GetAttribute("dir")))
                {
                    return string.Empty;
                }
                return _element.GetAttribute("dir");
            }
        }

        /// <summary>
        /// Set Disable on an element.  
        /// </summary>
        public void Disable()
        {
            if (!IsDisabled)
            {
                SetAttribute("disabled", "true");
            }
        }

        /// <summary>
        /// Double Click an Element.
        /// </summary>
        public void DoubleClick()
        {
            getElement();
            _log.Debug("Double clicking: " + MyBy);
            new Actions(Engine.WebDriver)
                .MoveToElement(_element, 0, 0)
                .DoubleClick(_element)
                .Perform();
            Thread.Sleep(100);
            Support.WaitForPageReadyState();
        }

        /// <summary>
        /// Double Click an Element at x,y offset
        /// </summary>
        public void DoubleClickAt(int x = 1, int y = 1)
        {
            getElement();
            _log.Debug("Double clicking: " + MyBy);
            new Actions(Engine.WebDriver)
                .MoveToElement(_element, 0, 0)
                .MoveByOffset(x, y)
                .DoubleClick(_element)
                .Perform();
            Thread.Sleep(100);
            Support.WaitForPageReadyState();
        }

        /// <summary>
        /// Drag and Drop to another Control.   
        /// </summary>
        /// <param name="droppableControl"></param>
        public void DragDropToControl(Control droppableControl)
        {
            getElement(highlight: false, refreshElement:true);
            if (_element == null)
                throw new Exception("No Element Found with " + MyBy);

            var droppableElement = droppableControl.Element;
            if (droppableElement == null)
                throw new Exception("No Element Found with " + MyBy);

            _log.Debug("Dragging and dropping: " + MyBy + " to " + droppableControl.MyBy);
            new Actions(Engine.WebDriver)
               .ClickAndHold(_element)
               .MoveToElement(droppableElement)
               .Release(droppableElement)
               .Build()
               .Perform();
            Thread.Sleep(100); // for javascript
            Support.WaitForPageReadyState();
        }

        /// <summary>
        /// Drag and Drop to another Element.   
        /// </summary>
        /// <param name="droppableElement"></param>
        public void DragDropToControl(IWebElement droppableElement)
        {
            getElement(highlight: false, refreshElement: true);
            if (_element == null)
                throw new Exception("No Element Found with " + MyBy);


            _log.Debug("Dragging and dropping: " + MyBy );
            new Actions(Engine.WebDriver)
               .ClickAndHold(_element)
               .MoveToElement(droppableElement)
               .Release(droppableElement)
               .Build()
               .Perform();
            Thread.Sleep(100); // for javascript
            Support.WaitForPageReadyState();
        }


        /// <summary>
        /// Set an element to enabled. (remove disabled from HTML tag)
        /// </summary>
        public void Enable()
        {
            if (!IsDisabled) return;

            getElement();
            Engine.Execute<object>(Engine.WebDriver, "arguments[0].removeAttribute('disabled')", _element);
        }

        /// <summary>
        /// Edit the Text of an element (option for click first)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clickFirst"></param>
        /// <returns>bool if value was verified to have changed to input value</returns>
        public bool EditText(string text, bool clickFirst = false)
        {
            getElement();
            if (clickFirst)
            {
                _element.Click();
                Thread.Sleep(10);
                Support.WaitForPageReadyState();
                getElement(highlight: false);
                _element.Clear();
                _element.SendKeys(text);
            }

            getElement(highlight: false);
            var presentValue = _element.GetAttribute("value");
            return text == presentValue;
        }

        /// <summary>
        /// This is the _element that is set with many of the other methods in this Class file.
        /// </summary>
        IWebElement _element;


        /// <summary>
        /// This is the IWebElement that is returned for this element.
        /// </summary>
        public IWebElement Element
        {
            get
            {
                getElement(refreshElement: true);
                return _element;
            }
        }

        /// <summary>
        /// This is the _element Collection that is set with many of the other methods in this Class file.
        /// </summary>
        System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> _elements;

        /// <summary>
        /// This is the IWebElement Collection that is returned for this element.
        /// </summary>
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> Elements
        {
            get
            {
                getElements();
                return _elements;
            }
        }

        /// <summary>
        /// Get Element as an Control child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public Control GetChildControl(OpenQA.Selenium.By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return new Control(Engine.FindChildElement(_element, by), by);
        }

        /// <summary>
        /// Get Element as an Control child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public Control GetChildControl(By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return new Control(Engine.FindChildElement(_element, by), by);
        }

        /// <summary>
        /// Get Element as an Control child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public Control GetChildControl(CustomBy by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return new Control(Engine.FindChildElement(_element, by));
        }

        /// <summary>
        /// Get Element as an IWebElement child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public IWebElement GetChildElement(OpenQA.Selenium.By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElement(_element, by);
        }

        /// <summary>
        /// Get Element as an IWebElement child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public IWebElement GetChildElement(By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElement(_element, by);
        }

        /// <summary>
        /// Get Element as an IWebElement child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public IWebElement GetChildElement(CustomBy by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElement(_element, by);
        }

        /// <summary>
        /// Get Multiple Elements as an IWebElement Collection child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> GetChildElementCollection(
            OpenQA.Selenium.By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElements(_element, by);
        }

        /// <summary>
        /// Get Multiple Elements as an IWebElement Collection child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> GetChildElementCollection(By by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElements(_element, by);
        }

        /// <summary>
        /// Get Multiple Elements as an IWebElement Collection child By inputted.
        /// </summary>
        /// <param name="by"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public System.Collections.ObjectModel.ReadOnlyCollection<IWebElement> GetChildElementCollection(CustomBy by, bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode, refreshElement: true);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            return Engine.FindChildElements(_element, by);
        }

        /// <summary>
        /// Get the Attribute Value of the attribute named.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public string GetAttributeValue(string attributeName, bool safeMode = false)
        {
            try
            {
                _element = Engine.WebDriver.FindElement(MyBy);
            }
            catch (Exception ex)
            {
                _log.Exception(ex);
                return null;
            }
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            return _element?.GetAttribute(attributeName);
        }

        /// <summary>
        /// Tests whether an element has an attribute
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="safeMode"></param>
        /// <returns></returns>
        public bool HasAttribute(string attributeName, bool safeMode = false)
        {
            try
            {
                _element = Engine.WebDriver.FindElement(MyBy);
            }
            catch
            {
                return false;
            }
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            return _element?.GetAttribute(attributeName) != null;
        }

        /// <summary>
        /// The GetAttribute function and the Text property both return strings. Many times these strings need to be parsed into other types. 
        /// Thus we can write a function to wrap that functionality for us.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public T GetAttributeAsType<T>(string attributeName)
        {
            getElement(highlight: false, refreshElement: true);
            var value = _element.GetAttribute(attributeName) ?? string.Empty;
            return (T) TypeDescriptor.GetConverter(typeof (T)).ConvertFromString(value);
        }

        /// <summary>
        /// Get the CSS Value of the property named
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public string GetCssValue(string propertyName)
        {
            getElement(highlight: false, refreshElement: true);
            return _element.GetCssValue(propertyName);
        }

        /// <summary>
        /// Get Element as a Control grandparent (2 levels up xpath)
        /// </summary>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public Control GetGrandParentControl(bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            try
            {
                var xpath = XPath;
                var by = OpenQA.Selenium.By.XPath(xpath + "/../..");
                var parentElement = Engine.FindElement(by, safeMode: false);
                return new Control(parentElement, by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        /// <summary>
        /// Get Element as an IWebElement grandparent (2 levels up xpath)
        /// </summary>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public IWebElement GetGrandParentElement(bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            try
            {
                var xpath = XPath;
                var by = OpenQA.Selenium.By.XPath(xpath + "/../..");
                var parentElement = Engine.FindElement(by, safeMode: false);
                return parentElement;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        /// <summary>
        /// Get Control as an Control(IWebElement) parent (1 levels up xpath)
        /// </summary>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public Control GetParentControl(bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            try
            {
                var xpath = XPath;
                var by = OpenQA.Selenium.By.XPath(xpath + "/..");
                var parentElement = Engine.FindElement(by, safeMode: false);
                return new Control(parentElement, by);
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        /// <summary>
        /// Get Element as an IWebElement parent (1 levels up xpath)
        /// </summary>
        /// <param name="safeMode">Do not return NoSuchElementException</param>
        /// <returns></returns>
        public IWebElement GetParentElement(bool safeMode = false)
        {
            getElement(highlight: false, safeMode: safeMode);
            if (_element == null && safeMode == false)
                throw new NoSuchElementException();
            if (_element == null)
                return null;
            try
            {
                var xpath = XPath;
                var by = OpenQA.Selenium.By.XPath(xpath + "/..");
                var parentElement = Engine.FindElement(by, safeMode: false);
                return parentElement;
            }
            catch (NoSuchElementException)
            {
                if (safeMode)
                    return null;
                throw;
            }
        }

        /// <summary>
        /// Wait until element is present, Get the element and save it as _element
        /// </summary>
        /// <param name="highlight">This is good for watching the automation</param>
        /// <param name="safeMode">safe = no exception if it's not found. just NULL</param>
        /// <param name="refreshElement">Default is to cache the element. This can have issues javascript changing TXT, and the method here not seeing it.</param>
        void getElement(bool highlight = true, bool safeMode = false, bool refreshElement = false)
        {
            if (_element != null && !isStale(_element) && !refreshElement)
            {
                return;
            }
            try
            {
                if (MyBy == null)
                {
                    Support.ScreenShot();
                    throw new ArgumentNullException("by cannot be null : HowUsed {" + HowUsed + "}");
                }
                if (how == How.XPath)
                    AssertValidXpath(selector);

                Engine.CurrentControl = this;
                var wait = new WebDriverWait(Engine.WebDriver, TimeSpan.FromSeconds(8));
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(MyBy));
                }
                catch (WebDriverTimeoutException)
                {
                    _log.Debug("Timed out looking for " + selector);
                }
                _element = Engine.WebDriver.FindElement(MyBy);
                Support.WaitForPageReadyState();
                if (highlight && _element != null)
                {
                    Task.Run(() => Highlight(_element));
                }
            }
            catch (NoSuchElementException)
            {
                if (safeMode == false && _element == null)
                {
                    Support.ScreenShot();
                    throw new NoSuchElementException(MyBy + " was not found on the page.");
                }
            }
            catch (WebDriverException webDriverException)
            {
                _log.Exception(webDriverException);
                throw;
            }
            catch (XPathException xPathException)
            {
                _log.Exception(xPathException);
                throw;
            }
            catch (Exception ex)
            {
                _log.Exception(ex);
                //throw;
            }
        }

        /// <summary>
        /// Wait until element is present, Get the element and save it as _element
        /// </summary>
        void getElement(How how, string childSelector)
        {
            if (_element != null && !isStale(_element) )
            {
                return;
            }
            try
            {
                if (how == How.XPath)
                    AssertValidXpath(selector);

                Engine.CurrentControl = this;
                _wait.Until(d => Engine.FindElement(how, childSelector));
                _element = Engine.FindElement(how, childSelector);
                Highlight();
            }
            catch (WebDriverException webDriverException)
            {
                _log.Exception(webDriverException);
                throw;
            }
            catch (XPathException xPathException)
            {
                _log.Exception(xPathException);
                throw;
            }
            catch (Exception ex)
            {
                _log.Exception(ex);
                throw;
            }
        }

        /// <summary>
        /// Wait until element is present, Get the elements and save it as _elements
        /// </summary>
        void getElements(bool safeMode = false, bool refreshElement = false)
        {
            Support.WaitForPageReadyState();
            if (_elements != null && !isStale(_element) && !refreshElement)
            {
                return;
            }
            try
            {
                if (how == How.XPath)
                    AssertValidXpath(selector);

                Engine.CurrentControl = this;
                //Support.DetectErrors();
                var wait = new WebDriverWait(Engine.WebDriver, TimeSpan.FromSeconds(4));
                try
                {
                    wait.Until(ExpectedConditions.ElementExists(MyBy));
                }
                catch (WebDriverTimeoutException)
                {
                    _log.Debug("Timed out looking for " + selector);
                }
                _elements = Engine.WebDriver.FindElements(MyBy);
            }
            catch (NoSuchElementException)
            {
                if (safeMode == false && _elements == null)
                {
                    Support.ScreenShot();
                    throw new NoSuchElementException(MyBy + " was not found on the page.");
                }
            }
            catch (WebDriverException webDriverException)
            {
                _log.Exception(webDriverException);
                throw;
            }
            catch (XPathException xPathException)
            {
                _log.Exception(xPathException);
                throw;
            }
            catch (Exception ex)
            {
                _log.Exception(ex);
                //throw;
            }
        }

        /// <summary>
        /// Get the IWebDriver associated with the element.
        /// </summary>
        public IWebDriver GetWebDriver
        {
            get
            {
                getElement(highlight: false);
                IWebDriver driver = ((IWrapsDriver)_element).WrappedDriver;
                return driver;
            }
        }

        /// <summary>
        /// Fire Event like blur
        /// </summary>
        /// <param name="eventMethod"></param>
        public void FireEvent(string eventMethod = "blur")
        {
            getElement(highlight: false);
            if (_element == null)
                throw new NoSuchElementException();
            try
            {
                _log.Debug("Attempting run of return arguments[0]." + eventMethod + "() could not be fired");
                Engine.Execute<object>(Engine.WebDriver, "return arguments[0]." + eventMethod + "()", _element);
            }
            catch(Exception ex)
            {
                _log.Error("return arguments[0]." + eventMethod + "() could not be fired", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Returns the Style font size for the element
        /// </summary>
        public string FontFamily
        {
            get
            {
                getElement(highlight: false);
                var result = Engine.Execute<string>(Engine.WebDriver, "return  document.defaultView.getComputedStyle(arguments[0],null).getPropertyValue('font-family')", _element);
                return result ?? "";
            }
        }

        /// <summary>
        /// Returns the Style font size for the element
        /// </summary>
        public string FontSize
        {
            get
            {
                getElement(highlight: false);
                var result = Engine.Execute<string>(Engine.WebDriver, "return  document.defaultView.getComputedStyle(arguments[0],null).getPropertyValue('font-size')", _element);
                return result ?? "";
            }
        }

        /// <summary>
        /// Using javasript, returns the id of an element.
        /// </summary>
        public string Id
        {
            get
            {
                getElement();
                var result = _element.GetAttribute("id");
                return result ?? "";
            }
        }

        /// <summary>
        /// Using javasript, returns the innerHTML of an element.
        /// </summary>
        string InnerHtmle(IWebElement element)
        {
            return element.GetAttribute("innerHTML");

          //  var result = Engine.Execute<string>(Engine.WebDriver, "return arguments[0].innerHTML", element);
          //  return result ?? "";
        }

        /// <summary>
        /// Using GetAttribute("innerHTML");, returns the innerHTML of an element.
        /// </summary>
        public string InnerHtml
        {
            get
            {
                getElement(highlight: false);
                return _element.GetAttribute("innerHTML");
             //   var result = Engine.Execute<string>(Engine.WebDriver, "return arguments[0].innerHTML", _element);
             //   return result ?? "";
            }
        }

        /// <summary>
        /// Returns if the element is a Button defined by: input type# text
        /// </summary>
        public bool IsButton
        {
            get
            {
                getElement();
                if (_element.TagName == "input" && _element.GetAttribute("type") == "submit")
                {
                    return true;
                }
                // optional kind of button = li > a
                if (_element.TagName == "li")
                {
                    var ahref = Engine.FindChildElements(_element, OpenQA.Selenium.By.TagName("a"));
                    if (ahref.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Return true/false if the Checkbox is checked. If displayed = false, it will still check for the value.
        /// </summary>
        public bool IsChecked
        {
            get
            {
                getElement(refreshElement: true);
                if (_element.Displayed)
                {
                    return _element.Selected || _element.GetAttribute("checked") == "true" || _element.GetAttribute("checked") == "checked";
                }
                _log.Error(MyBy + " is not Displayed");
                return _element.GetAttribute("checked") == "true";
            }
        }

        /// <summary>
        /// Return true/false if the element is enabled. If displayed = false, it will still check for the value.
        /// </summary>
        public bool IsDisabled
        {
            get
            {
                getElement();
                if (_element.Displayed)
                {
                    _log.Debug(MyBy + " is Disabled");
                    return !_element.Enabled;
                }
                _log.Error(MyBy + " is not Disabled");
                return !_element.Enabled;
            }
        }

        /// <summary>
        /// Returns if Selenium feels the element is Displayed.
        /// The OpenQA.Selenium.IWebElement.Displayed property avoids the problem of having to parse an element's "style" attribute to determine visibility of an element.
        /// </summary>
        public bool IsDisplayed
        {
            get
            {
                try
                {
                    Support.WaitForPageReadyState();
                    _element = Engine.WebDriver.FindElement(MyBy);
                }
                catch
                {
                    return false;
                }

                if (_element == null)
                    return false;
                if (_element.Displayed)
                {
                    _log.Debug(MyBy + " is displayed");
                    return true;
                }
                _log.Error(MyBy + " is not displayed");
                return false;
                //return _element.Displayed;
            }
        }

        /// <summary>
        /// Returns indication that this is an editable field and Enabled
        /// </summary>
        /// <returns></returns>
        public bool IsEditable
        {
            get
            {
                try
                {
                    getElement();
                    var hasClass = _element.GetAttribute("class").Contains("editable");
                    return hasClass && _element.Enabled;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns if the Element is visibility=Hidden, opacity=0, type=hidden, Displayed
        /// </summary>
        /// <returns></returns>
        public bool IsElementVisible
        {
            get
            {
                if (MyBy == null)
                    return false;
                try
                {
                    Support.WaitForPageReadyState();
                    _element = Engine.WebDriver.FindElement(MyBy);
                }
                catch
                {
                    return false;
                }
                
                if (_element == null)
                    return false;
                _log.Debug("IsElementVisible: " + selector);

                var visibilityString = _element.GetAttribute("visibility");
                if (visibilityString != null && !string.IsNullOrEmpty(visibilityString))
                {
                    if (visibilityString.ToLower() == "hidden")
                    {
                        return false;
                    }
                }

                try
                {
                    var transform = Engine.Execute<object>(Engine.WebDriver, "arguments[0].style.transform = 'none';",
                        _element);
                    if (transform != null && (bool)transform)
                    {
                        return false;
                    }
                }
                catch
                {
                    //ignore
                }
                var attributeType = _element.GetAttribute("type");
                if (attributeType != null && !string.IsNullOrEmpty(attributeType))
                {
                    if (attributeType.ToLower() == "hidden")
                    {
                        return false;
                    }
                }
                var opacity = _element.GetAttribute("opacity");
                if (opacity != null && !string.IsNullOrEmpty(opacity))
                {
                    if (opacity.ToLower() == "0")
                    {
                        return false;
                    }
                }
                return _element.Displayed;
            }
        }

        /// <summary>
        /// Returns if the Element is in the Correct Location 
        /// </summary>
        /// <param name="point">System.Draw.Point</param>
        /// <returns></returns>
        public bool IsElementInCorrectLocation(Point point)
        {
            getElement(highlight: false);
            return _element.Location == point;
        }

        /// <summary>
        /// Returns if the Element is in the Correct Location 
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns></returns>
        public bool IsElementInCorrectLocation(int x, int y)
        {
            var point = new Point
            {
                X = x,
                Y = y
            };
            getElement(highlight: false);
            return _element.Location == point;
        }

        /// <summary>
        /// Returns if the Element is in the Correct Location 
        /// </summary>
        /// <param name="topX">int</param>
        /// <param name="topY">int</param>
        /// <param name="bottomX">int</param>
        /// <param name="bottomY">int</param>
        /// <returns></returns>
        public bool IsElementInCorrectLocation(int topX, int topY, int bottomX, int bottomY)
        {

            getElement(highlight: false);

            if ((topX < _element.Location.X) &&
                (bottomX > _element.Location.X) &&
                (topY < _element.Location.Y) &&
                (bottomY > _element.Location.Y))
            {
                return true;
            }
            // Log error for debugging needs.
            _log.Error("Element is in wrong location." + Environment.NewLine +
                             "Element is at X:" + _element.Location.X + " Y:" + _element.Location.Y);
            return false;
        }

        /// <summary>
        /// Return true/false if the element is enabled. If displayed = false, it will still check for the value.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                getElement(highlight: false);
                if (_element.Displayed)
                {
                    _log.Debug(MyBy + " is Enabled");
                    return _element.Enabled;
                }
                _log.Error(MyBy + " is not Enabled");
                return _element.Enabled;
            }
        }

        /// <summary>
        /// Returns if the Element is visibility=Hidden Or not.
        /// </summary>
        /// <returns></returns>
        public bool IsHidden
        {
            get
            {
                var visibilityString = GetAttributeValue("visibility");
                if (visibilityString != null && !string.IsNullOrEmpty(visibilityString))
                {
                    if (visibilityString.ToLower() == "hidden")
                    {
                        return true;
                    }
                }
                var hiddenString = GetCssValue("display");
                if (hiddenString != null && !string.IsNullOrEmpty(hiddenString))
                {
                    if (hiddenString.ToLower() == "hidden")
                    {
                        return true;
                    }
                }
                try
                {
                    getElement();
                    if (_element.Displayed)
                    {
                        return false;
                    }
                }
                catch
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns if the element is a RichTextEditor 
        /// </summary>
        public bool IsRichTextEditor
        {
            get
            {
                getElement(highlight: false);
                if (_element.TagName == "div")
                {
                    if (_element.GetAttribute("class").Contains("multiline"))
                         return true;

                    var textAreaElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("textarea"));
                    if (textAreaElement != null 
                        && textAreaElement.GetAttribute("class").Contains("richtext"))
                    {
                        return true;
                    }

                    if (textAreaElement != null
                        && textAreaElement.GetAttribute("class").Contains("multiline"))
                    {
                        return true;
                    }

                    _element.Click();
                    Thread.Sleep(100);
                    var textAreaElements =
                        _element.FindElements(OpenQA.Selenium.By.ClassName("redactor-editor"));
                    if (textAreaElements.Count > 0)
                    {
                        return true;
                    }
                }
                return _element.GetAttribute("class").Contains("redactor-editor");
            }
        }

        /// <summary>
        /// Return true/false if the Selected element has a condition of selected
        /// </summary>
        public bool IsSelected
        {
            get
            {
                getElement(refreshElement: true);
                if (_element.Selected)
                {
                    _log.Debug(MyBy + " is Selected");
                    return _element.Selected;
                }
                _log.Error(MyBy + " is not Selected");
                Support.ScreenShot();
                return _element.Selected;
            }
        }

        /// <summary>
        /// Stale References happen when a page is refreshed, and the element call is not refreshed.
        /// </summary>
        public bool IsStaleReference
        {
            get
            {
                try
                {
                    // Calling any method forces a staleness check
                    // ReSharper disable once UnusedVariable
                    var enabled = _element.Enabled;
                    return false;
                }
                catch (StaleElementReferenceException)
                {
                    // expected to have this exception
                    return true;
                }
            }
        }

        /// <summary>
        /// Stale References happen when a page is refreshed, and the element call is not refreshed.
        /// </summary>
        bool isStale(IWebElement element)
        {
            try
            {
                // Calling any method forces a staleness check
                // ReSharper disable once UnusedVariable
                var enabled = element.TagName;
                return false;
            }
            catch (StaleElementReferenceException)
            {
                // expected to have this exception
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// Stale References happen when a page is refreshed, and the element call is not refreshed.
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        bool isStale(IEnumerable<IWebElement> elements)
        {
            try
            {
                // Calling any method forces a staleness check
                // ReSharper disable once UnusedVariable
                var enabled = elements.First().Enabled;
                return false;
            }
            catch (StaleElementReferenceException)
            {
                // expected to have this exception
                return true;
            }
        }

        /// <summary>
        /// Returns if the element is a TextBox defined by: input type# text
        /// </summary>
        public bool IsTextBox
        {
            get
            {
                getElement(highlight: false);
                if (_element.TagName == "input" && _element.GetAttribute("type") == "text")
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Check against text input to see if it is contained in the Text of the element.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool IsTextPresent(string text)
        {
            try
            {
                getElement(highlight: false);
                if (_element.Text != null)
                {
                    return _element.Text.ToLower().Contains(text.ToLower());
                }
                return false;
            }
            catch (NoSuchElementException)
            {
                // expected to have this exception
                return false;
            }
        }

        /// <summary>
        /// Returns if the element is or has a ul with mcdropdown_menu in the class name.
        /// </summary>
        public bool IsUberDropdown
        {
            get
            {
                getElement(highlight: false);
                if (_element.TagName == "div")
                {
                    var dropdownInput = Engine.FindChildElements(_element, OpenQA.Selenium.By.ClassName("mcdropdown"));
                    if (dropdownInput != null && dropdownInput.Count > 0)
                    {
                        return true;
                    }
                    var ulElement = Engine.FindChildElements(_element, OpenQA.Selenium.By.ClassName("mcdropdown_menu"));
                    if (ulElement != null && ulElement.Count > 0)
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(_element.GetAttribute("data-list-name")))
                    {
                        return true;
                    }

                }
                if (_element.TagName == "input")
                {
                    if (_element.GetAttribute("class").Contains("uber"))
                    {
                        return true;
                    }
                }

                return _element.TagName == "ul" && _element.GetAttribute("class").Contains("mcdropdown_menu");
            }
        }

        /// <summary>
        /// Return if the _element is null or not
        /// //copy of NotNull
        /// </summary>
        public bool IsNull
        {
            get { return _element == null; }
        }

        /// <summary>
        /// Return true/false if the element has been found on the page in any visibility condition.
        /// </summary>
        public bool IsValidOnPage
        {
            get
            {
                Engine.CurrentControl = this;
                try
                {
                    Engine.WebDriver.FindElement(MyBy);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Same code as Displayed, but can be made different later with javascript
        /// </summary>
        public bool IsVisible
        {
            get
            {
                try
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                }
                catch
                {
                    return false;
                }

                if (_element == null)
                    return false;

                //if (_element.Displayed)
                //{
                //    _log.Debug(MyBy + " is displayed");
                //}
                //else
                //{
                //    _log.Error(MyBy + " is not displayed");
                //    Support.ScreenShot();
                //}
                return _element.Displayed;
            }
        }

        /// <summary>
        /// Set the Style of the Element
        /// </summary>
        /// <param name="style"></param>
        public void SetStyle(string style)
        {
            SetAttribute("style", style);
        }

        /// <summary>
        /// Looks for Class string inside Class Attribute via Selenium.
        /// </summary>
        /// <param name="class"></param>
        /// <returns></returns>
        public bool HasClass(string @class)
        {
            getElement(highlight: false, safeMode: true, refreshElement: true);
            if (_element == null)
            {
                _log.Debug("HasClass Method was passed a null element. Elements need to be present before use.");
                throw new Exception("HasClass was passed a null element. class= " + @class);
            }
            try
            {
                return _element.GetAttribute("class").Contains(@class);
            }
            catch (StaleElementReferenceException )
            {
                _element = Support.WaitUntilElementIsPresent(MyBy);
                return _element.GetAttribute("class").Contains(@class);
            }
        }

        /// <summary>
        /// Returns what is causing the element to be hidden
        /// </summary>
        /// <returns></returns>
        public static IWebElement HiddenByElement(IWebElement hiddenElement)
        {
            if (hiddenElement == null) throw new NullReferenceException("hiddenElement was passed null");

            //base element
            var ifDisplayed = hiddenElement.Displayed;
            if (ifDisplayed)
                return null;

            IWebElement parent = hiddenElement;
            while (ifDisplayed == false)
            {
                parent = parent.FindElement(OpenQA.Selenium.By.XPath(".."));
                ifDisplayed = parent.Displayed;
            }
            return parent;
        }

        /// <summary>
        /// Returns what is causing the element to be hidden
        /// </summary>
        /// <returns></returns>
        public Control HiddenByElement()
        {
            getElement(highlight: false);
            var parent = HiddenByElement(_element);
            if (parent == null)
                return null;
            return new Control(parent);
        }

        /// <summary>
        /// Highlight the element with default coloring style. using javascript
        /// default to: "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;"
        /// </summary>
        /// <param name="element"></param>
        /// <param name="style"></param>
        public void Highlight(IWebElement element,
            string style = "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;")
        {
            try
            {
                Engine.Execute<object>("arguments[0].setAttribute(arguments[1], arguments[2])", element, "style", style);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// Highlight the element with default coloring style.
        /// default to: "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;"
        /// </summary>
        public void Highlight(string style = "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;")
        {
            try
            {
                getElement(highlight: false);
                if (_element.Displayed)
                {
                    SetAttribute("style", style);
                }
                SetAttribute("style", style);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// Make the checkbox checked if the Checkbox is checked. Then uncheck it If displayed = false, it will still check for the value.
        /// </summary>
        public void UnCheck()
        {
            getElement();
            if (_element.Displayed)
            {
                if (_element.Selected || _element.GetAttribute("checked") == "true" || _element.GetAttribute("checked") == "checked")
                {
                    var action = new Actions(Engine.WebDriver);
                    var readyClick = action.MoveToElement(_element).Click().Build();
                    readyClick.Perform();
                    Thread.Sleep(1000);
                    return;
                }
                return;
            }
            // may need to throw error here later.
            _log.Error(MyBy + " is not Displayed");
            return;

        }

        /// <summary>
        /// UnHighlight the element with default coloring style.
        /// default to: "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;"
        /// </summary>
        public void UnHighlight(string style = "border:0px")
        {
            try
            {
                getElement(highlight: false);
                if (_element.Displayed)
                {
                    SetAttribute("style", style);
                }
                SetAttribute("style", style);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// UnHighlight the element with default coloring style.
        /// default to: "background-color:#F7E4B0;color:#6B500A;border:2px solid #EEC454;"
        /// </summary>
        public void UnHighlight(IWebElement element, string style = "border:0px")
        {
            try
            {
                if (element.Displayed)
                {
                    SetAttribute("style", style);
                }
                SetAttribute("style", style);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        /// Simulates a Mouse HoverOver using Selenium Action
        /// </summary>
        /// <param name="javaScript"></param>
        public void HoverOver(bool javaScript = true)
        {
            try
            {
                if (javaScript)
                {
                    const string script = "var evObj = document.createEvent('MouseEvents');" +
                                          "evObj.initMouseEvent(\"mouseover\",true, false, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);" +
                                          "arguments[0].dispatchEvent(evObj);";
                    Engine.Execute<object>(Engine.WebDriver, script, _element);
                }
                else
                {
                    getElement(highlight: false);
                    new Actions(Engine.WebDriver)
                        .MoveToElement(_element, 1, 1)
                        .Build()
                        .Perform();
                }
            }
            catch (Exception)
            {
                //ignore all errors on Hover.
            }
           
        }

        /// <summary>
        /// using jQuery Hover over an Item.
        /// </summary>
        /// <param name="jquerySelector"></param>
        public static void HoverOverjQuery(string jquerySelector)
        {
            var javaScript = @"$('" + jquerySelector + "').hover();";
            Engine.ExecuteJavaScript(javaScript);
        }


        /// <summary>
        /// Return the HREF Attribute for the element
        /// </summary>
        public string Href
        {
            get
            {
                getElement(highlight: false);
                return _element.GetAttribute("href");
            }
        }

        /// <summary>
        /// Gets a System.Drawing.Point object containing the coordinates of the upper-left corner of this element relative to the upper-left corner of the page.
        /// </summary>
        public Point Location
        {
            get
            {
                getElement(highlight: false);
                _log.Debug("Location of element : " + MyBy + " is " + _element.Location);
                return _element.Location;
            }
        }

        /// <summary>
        /// Sets the Visibility css for the element to visible
        /// </summary>
        public void MakeVisible()
        {
            SetAttribute("visibility", "visible");
        }

        /// <summary>
        /// Preference for Invisible or Hidden, up to user.
        /// Sets the Visibility CSS for the element to hidden
        /// </summary>
        public void MakeHidden()
        {
            getElement(highlight: false);
            if (!_element.Displayed)
                return;  // already hidden

            var style = _element.GetAttribute("style");

            if (!style.Contains("display"))
                MakeInVisible();

            if (style.Contains("display: block"))
                SetStyle(style.Replace("display: block", "display: hidden").Replace("display:block", "display:hidden"));

        }

        /// <summary>
        /// Preference for Invisible or Hidden, up to user.
        /// Sets the Visibility CSS for the element to hidden
        /// </summary>
        public void MakeInVisible()
        {
            SetAttribute("visibility", "hidden");
        }

        /// <summary>
        /// Return the computed Margin value
        /// </summary>
        public string Margin
        {
            get
            {
                getElement(highlight: false);
                var result = Engine.Execute<string>(Engine.WebDriver, "return  document.defaultView.getComputedStyle(arguments[0],null).getPropertyValue('margin')", _element);
                return result ?? "";
            }
        }

        /// <summary>
        /// Gets the maxlength for a field from the GetAttribute
        /// </summary>
        public string MaxLength
        {
            get
            {
                getElement(highlight: true);
                return _element.GetAttribute("maxlength");
            }
        }

        /// <summary>
        /// When there is a menu-open class for toggling, etc this makes sure that control opens.
        /// </summary>
        public void MenuOpen()
        {
            Support.WaitForPageReadyState(TimeSpan.FromSeconds(16));
            //getElement();
            if (HasClass("menu-open"))
                return; // menu is already open.
            try
            {
                var ahref = GetChildControl(OpenQA.Selenium.By.TagName("a"));
                ahref.Click();

                // wait for javascript ::after to setup.
                Thread.Sleep(10);
                if (HasClass("menu-open"))
                    return; // menu has opened.

                AddClass("menu-open");
                if (HasClass("menu-open"))
                    return; // menu has opened.
                throw new Exception("Menu would not open for this Control");
            }
            catch(Exception ex)
            {
                _log.Error(MyBy + " could not be clicked", ex);
                Support.ScreenShot();
                throw;
            }
        }


        /// <summary>
        /// Mouse over is an action sequence. 
        /// </summary>
        public void MouseOver()
        {
            HoverOver();
        }

        /// <summary>
        /// Using javascript or Selenium Action moveToElement (hover/mouseover)
        /// </summary>
        /// <param name="javaScript"></param>
        public void MoveToElement(bool javaScript = true)
        {
            if (javaScript)
            {
                const string script = "var evObj = document.createEvent('MouseEvents');" +
                                      "evObj.initMouseEvent(\"mouseover\",true, false, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);" +
                                      "arguments[0].dispatchEvent(evObj);";
                Engine.Execute<object>(Engine.WebDriver, script, _element);
            }
            else
            {
                getElement();
                new Actions(Engine.WebDriver)
                    .MoveToElement(_element, 0, 0)
                    .Build()
                    .Perform();
            }
        }

        /// <summary>
        /// Using javascript, returns the id of an element.
        /// </summary>
        public string Name
        {
            get
            {
                getElement(highlight: false);
                if (_element == null)
                {
                    Support.ScreenShot();
                    throw new NoSuchElementException(MyBy + " was not found on the page.");
                }
                var result = _element.GetAttribute("name");
                return result ?? "";
            }
        }

        /// <summary>
        /// Return if the _element is null or not
        /// </summary>
        public bool NotNull
        {
            get { return _element != null; }
        }


        /// <summary>
        /// This method is mostly for extremely large text.  Like on the SAML tests
        /// </summary>
        /// <param name="value"></param>
        public void PasteText(string value)
        {
            getElement();
            try
            {
                var action = new Actions(Engine.WebDriver);
                var readyClick = action.MoveToElement(_element).Click().Build();
                readyClick.Perform();

                var currentValue = _element.Text;
                _element.Clear();

                if (string.IsNullOrWhiteSpace(value))
                {
                    _log.Error("Value trying to be entered is blank, skipping since data is already cleared.:  " + MyBy);
                    return;
                }
                if (Engine.GetCurrentUrl.Contains("saml") ||
                    Engine.GetCurrentUrl.Contains("test") ||
                    Engine.GetCurrentUrl.Contains("idp"))
                {
                    Clipboard.SetText(value);
                    // paste content of clipboard to control by Shift-Insert
                    _element.SendKeys(Keys.Shift + Keys.Insert);

                   // Engine.Execute<object>("arguments[0].value='" + value + "'", _element);
                }
                else if (value.Length <= 2000)
                {
                    _log.Info("TextBox pasteText from: " + currentValue + " to " + value);
                    _element.SendKeys(value);
                }
                else
                {
                    Engine.Execute<object>("arguments[0].value='" + value + "'", _element);
                }
            }
            catch (Exception ex)
            {
                _log.Error("PasteText method " + MyBy, ex);
                throw;
            }
        }

        /// <summary>
        /// Remove a class from an element
        /// </summary>
        /// <param name="class"></param>
        public void RemoveClass(string @class)
        {
            getElement(highlight: false);
            if (_element == null)
            {
                Support.ScreenShot();
                throw new NoSuchElementException(MyBy + " was not found on the page.");
            }
            Engine.Execute<object>("arguments[0].classList.remove('" + @class + "');", _element);
        }

        /// <summary>
        /// Get the InnerHTML of the rich Text Editor (redactor)
        /// </summary>
        public string RichTextEditorInnerHtml
        {
            get
            {
                getElement(highlight: false);
                if (_element == null)
                {
                    Support.ScreenShot();
                    throw new NoSuchElementException(MyBy + " was not found on the page.");
                }
                if (_element.GetAttribute("class").Contains("redactor-editor"))
                {
                    return InnerHtmle(_element);
                }
                else
                {
                    var editor = Engine.FindChildElement(_element, OpenQA.Selenium.By.ClassName("redactor-editor"));
                    return InnerHtmle(editor);
                }
            }
        }

        /// <summary>
        /// Get the Value of the rich Text Editor (redactor)
        /// </summary>
        public string RichTextEditorValue
        {
            get
            {
                getElement(highlight: false);
                if (_element == null)
                {
                    Support.ScreenShot();
                    throw new NoSuchElementException(MyBy + " was not found on the page.");
                }
                if (_element.GetAttribute("class").Contains("redactor-editor"))
                {
                    return _element.Text;
                }
                else
                {
                    var editor = Engine.FindChildElement(_element, OpenQA.Selenium.By.ClassName("redactor-editor"));
                    return editor.Text;
                }
            }
        }

        /// <summary>
        /// Selenium Context Click (right click)
        /// </summary>
        public void RightClick()
        {
            getElement(highlight: false);

            new Actions(Engine.WebDriver)
                .MoveToElement(_element)
                .ContextClick(_element)
                .Build()
                .Perform();
        }


        /// <summary>
        /// mcdropdown_menu  Set the value via javascript.
        /// </summary>
        /// <param name="value"></param>
        public void SetUberListValue(string value)
        {
            var inlineEditable = false;

            const string dataListNameAttr = "data-list-name";
            getElement(refreshElement: true);
            if (_element == null)
            {
                Support.ScreenShot();
                throw new NoSuchElementException(MyBy + " was not found on the page.");
            }
            if (_element.GetAttribute("class").Contains("editable"))
            {
                inlineEditable = true;
                var action = new Actions(Engine.WebDriver);
                var readyClick = action.MoveToElement(_element).Click().Build();
                readyClick.Perform();

                Thread.Sleep(50); //for javascript
                getElement(refreshElement: true);
            }


            string dataListName ;

            var dropDownType = _element.GetAttribute("data-list-name");
            if (string.IsNullOrEmpty(dropDownType) && _element.TagName == "div")
            {
                // page isQuery Type
                var inputElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("input"));
                dataListName = inputElement.GetAttribute(dataListNameAttr);
                // Mcdropdown on the Query pages:
                Engine.Execute<object>("$('input[data-list-name=" + dataListName + "]').val('" +
                                       value.Replace(": ", ":") + "').trigger('selected');");
                Engine.Execute<object>("$('input[data-list-name=" + dataListName +
                                       "]').siblings('input[type=hidden]').val('" + value.Replace(": ", ":") +
                                       "').trigger('selected');");
            }
            else if (_element.TagName == "input")
            {
                // page is FAvalue type
                dataListName = _element.GetAttribute(dataListNameAttr);
                // Mcdropdown on the New Case page:
                Engine.Execute<object>("$('input[data-list-name=" + dataListName + "]').val('" +
                                       value.Replace(": ", ":") + "').trigger('selected');");
                Engine.Execute<object>("$('input[data-list-name=" + dataListName +
                                       "]').siblings('input[type=hidden]').val('" +
                                       value.Replace(": ", ":") + "').trigger('selected');");
            }
            else
            {
                // page is FAvalue type
                dataListName = _element.GetAttribute(dataListNameAttr);
                // Mcdropdown on the Case nav pages:
                Engine.Execute<object>("$('div[data-list-name=" + dataListName + "]').find('input[type=text]').val('" +
                                       value.Replace(": ", ":") + "').trigger('selected');");

                Engine.Execute<object>("$('div[data-list-name=" + dataListName +
                                       "]').find('input[type=text]').siblings('input[type=hidden]').val('" +
                                       value.Replace(": ", ":") + "').trigger('selected');");
            }
            if (inlineEditable)
            {
                try
                {
                    switch (Engine.Browser)
                    {
                        case SupportedBrowserType.Chrome:
                            Engine.FindElement(By.TagName("body")).SendKeys(Keys.Tab);
                            break;
                        case SupportedBrowserType.Firefox:
                        case SupportedBrowserType.Ie:
                            Engine.FindElement(By.TagName("html")).SendKeys(Keys.Tab);
                            break;
                    }
                }
                catch
                {
                    //ignore
                }
            }
            Support.WaitForPageReadyState();
            Support.WaitForAjaxToComplete();
        }

        /// <summary>
        /// Simulates typing text into the element.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clearFirst"></param>
        /// <param name="highlight"></param>
        public void SetTextValue(string text, bool clearFirst =  true, bool highlight = true)
        {
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("SendKeys " + selector, ts)))
            {
                Support.WaitForPageReadyState();
                WaitForElementVisible();

                getElement(highlight: highlight);
                try
                {
                    // check for editable (clickable) element textbox type
                    if (_element.TagName == "div" && _element.GetAttribute("class").Contains("editable"))  
                    {
                        try
                        {
                            var action = new Actions(Engine.WebDriver);
                            var readyClick = action.MoveToElement(_element).Click().Build();
                            readyClick.Perform();

                            Thread.Sleep(10);
                            getElement(); // re-get element.

                            var input =
                                Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("input"), safeMode: true) ??
                                Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("textarea"), safeMode: true);
                                //div[@id='" + name + "']/form/input
                            if (input == null)
                                throw new NoSuchElementException("No element found to Type in");

                            _log.Debug("Typing " + text + " in " + selector);
                            if (clearFirst)
                                input.Clear();
                            if (text != "BLANK")
                                input.SendKeys(text);
                            // The next 10 lines are just to try and get the control to trigger a save.
                            Thread.Sleep(10); //for javascript
                            input.SendKeys(Keys.Tab);
                            Thread.Sleep(100); //for javascript
                            Support.WaitForPageReadyState();

                            input =
                                Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("input"), safeMode: true) ??
                                Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("textarea"), safeMode: true);
                            if (input != null)
                            {
                                if (input.TagName == "textarea")
                                {
                                    input.Submit();
                                }
                                try
                                {
                                    Support.WaitForPageReadyState();
                                    switch (Engine.Browser)
                                    {
                                        case SupportedBrowserType.Chrome:
                                            Engine.FindElement(OpenQA.Selenium.By.TagName("body")).SendKeys(Keys.Tab);
                                            break;

                                        case SupportedBrowserType.Firefox:
                                        case SupportedBrowserType.Ie:
                                            Engine.FindElement(OpenQA.Selenium.By.TagName("html")).SendKeys(Keys.Tab);
                                            break;
                                    }
                                    getElement(); // re-get element.
                                    var parentElement = _element.FindElement(OpenQA.Selenium.By.XPath(".."));

                                    var clickParent = action.MoveToElement(parentElement).Click().Build();
                                    clickParent.Perform();

                                    Thread.Sleep(100);
                                    Support.WaitForPageReadyState();
                                }
                                catch (Exception)
                                {
                                    //ignore
                                }
                            }
                        }
                        catch (StaleElementReferenceException se)
                        {
                            _log.Error("SetTextValue stale ref error", se);
                        }
                    }
                    else
                    {
                        if (clearFirst) _element.Clear();
                        if (text != "BLANK")
                            _element.SendKeys(text);
                        _log.Debug("Typing " + text + " in " + selector);
                    }
                }
                catch (Exception ex)
                {
                    //if (ex.Message.Contains("Element not found in the cache")) ;
                    //{
                    //    SetTextValue(text, clearFirst);
                    //}
                    _log.Error("Unable to type " + text + " in " + selector, ex);
                    Support.ScreenShot();
                    throw;
                }
            }
        }

        /// <summary>
        /// Simulates typing text into the element.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clearFirst"></param>
        public void SetRichTextValue(string text, bool clearFirst = true)
        {
            var action = new Actions(Engine.WebDriver);
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("SendKeys " + selector, ts)))
            {
                getElement(highlight: false);
                try
                {
                    // check for editable (clickable) element textbox type
                    if (_element.TagName == "div" && (_element.GetAttribute("class").Contains("editable") || _element.GetAttribute("contenteditable") == "true"))
                    {

                        var regularClick = action.MoveToElement(_element).Click().Build();
                        regularClick.Perform();

                        Thread.Sleep(100);
                        getElement(highlight: false); // re-get element.

                        // if the BY for this is given as the actual redactor control.
                        if (_element.GetAttribute("class").Contains("redactor-editor"))
                        {
                            _log.Debug("Typing " + text + " in " + selector);
                            if (clearFirst)
                            {
                                _element.Clear();
                                _element.SendKeys(Keys.Backspace);
                            }
                            if (text != "BLANK")
                                _element.SendKeys(text);
                            Thread.Sleep(100);
                            try
                            {
                                var clickBody = action.MoveToElement(Engine.FindElement(By.TagName("body"))).Click().Build();
                                clickBody.Perform();
                            }
                            catch (Exception)
                            {
                                //ignore
                            }
                            
                        }
                        else // if the BY for this contains a redactor control somewhere below it.
                        {
                            var redactor = Engine.FindChildControl(parentElement: _element, by: OpenQA.Selenium.By.ClassName("redactor-editor"));  //div[@id='" + name + "']/form/input
                            if (redactor != null)
                            {
                                _log.Debug("Typing " + text + " in " + selector);
                                redactor.Click();
                                Thread.Sleep(200);
                                Support.WaitForPageReadyState();
                                redactor = Engine.FindChildControl(parentElement: _element, by: OpenQA.Selenium.By.ClassName("redactor-editor"));
                                if (clearFirst)
                                {
                                    redactor.Clear();
                                    redactor.SendKeys(Keys.Backspace);
                                }
                                if (text != "BLANK")
                                    redactor.SendKeys(text);
                                Thread.Sleep(100);
                                try
                                {
                                    var readyClick = action.MoveToElement(_element).Click().Build();
                                    readyClick.Perform();
                                }
                                catch (Exception)
                                {
                                    //ignore
                                }

                            }
                            else
                            {
                                throw new NoSuchElementException("No redactor-editor found.");
                            }
                        }
                    }
                    else
                    {
                        if (clearFirst) _element.Clear();
                        if (text != "BLANK")
                            _element.SendKeys(text);
                        Thread.Sleep(20);
                        _log.Debug("Typing " + text + " in " + selector);
                    }
                }
                catch (Exception ex)
                {
                    //if (ex.Message.Contains("Element not found in the cache")) ;
                    //{
                    //    SetTextValue(text, clearFirst);
                    //}
                    _log.Error("Unable to type " + text + " in " + selector, ex);
                    Support.ScreenShot();
                    throw;
                }
            }
        }

        /// <summary>
        /// Saves a screen shot of an element on current browser
        /// </summary>
        /// <param name="filename">input full filename=("path" + "name" + ".png")</param>
        public void SaveScreenShotOfElement(string filename = "f.png")
        {
            getElement(highlight: false);
            // Take ScreenCap of Entire Screen
            var screenshotDriver = Engine.WebDriver as ITakesScreenshot;
            Screenshot screenshot = screenshotDriver.GetScreenshot();
            var bmpScreen = new Bitmap(new MemoryStream(screenshot.AsByteArray));
            // Crop ScreenCap to Element
            var cropArea = new Rectangle(_element.Location, _element.Size);
            Bitmap bmpCrop = bmpScreen.Clone(cropArea, bmpScreen.PixelFormat);
            //Save
            if (filename.Length < 6)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-hhmm-ss");
                var screenShotFile = timestamp + "_" + Guid.NewGuid().ToString("N") + ".png";
                bmpCrop.Save(@"c:\storyteller_screenshots\" + screenShotFile, ImageFormat.Png);
            }
            else if (filename.Contains(@"\"))
                bmpCrop.Save(filename, ImageFormat.Png);
            else
                bmpCrop.Save(@"c:\storyteller_screenshots\" + filename, ImageFormat.Png);
        }


        /// <summary>
        /// This will use javascript to scroll to the element on the page.
        /// </summary>
        public void ScrollTooElement()
        {
            getElement(highlight: false);
            new Actions(Engine.WebDriver)
                                  .MoveToElement(
                                  _element, 0, 0)
                                  .Build()
                                  .Perform();
          //  const string script = "arguments[0].scrollIntoView(true);";
          //  var execute = Engine.Execute<object>(Engine.WebDriver, script, _element);
          //  Thread.Sleep(10);
        }

        /// <summary>
        /// This will use javascript to scroll to the element on the page.
        /// </summary>
        /// <param name="element"></param>
        public void ScrollTooElement(IWebElement element)
        {

            new Actions(Engine.WebDriver)
                      .MoveToElement(
                      element, 0, 0)
                      .Build()
                      .Perform();

          //  const string script = "arguments[0].scrollIntoView(true);";
          //  var execute = Engine.Execute<object>(Engine.WebDriver, script, _element);
          //  Thread.Sleep(10);
        }

        /// <summary>
        /// Gets the Src of the Image element
        /// example: <img src="/agent/_content/images/dt-logo-trans.png"/>  
        /// </summary>
        public string Src
        {
            get
            {
                getElement(highlight: false);
                return _element.GetAttribute("src");
            }
        }

        /// <summary>
        ///  This will select the element by .SelectByText in Selenium  
        /// </summary>
        /// <param name="value"></param>
        public void Select(string value)
        {
            getElement();
            try
            {
                _log.Debug(value + " selected");
                var select = new SelectElement(_element);
                select.SelectByText(value);
            }
            catch
            {
                _log.Debug(value + " could not be selected");
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        ///  This will select the element by .SelectByIndex in Selenium  
        /// </summary>
        /// <param name="index"></param>
        public void SelectByPosition(int index)
        {
            getElement(highlight: false);
            try
            {
                _log.Debug(index + " selected");
                var select = new SelectElement(_element);
                select.SelectByIndex(index);
            }
            catch
            {
                _log.Error(index + " could not be selected");
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// This will loop each option value until it finds the subText, then clicks on the Text.
        /// If it does not find the text on first pass, it will try SelectByText option.
        /// </summary>
        /// <param name="subText"></param>
        /// <returns></returns>
        public string SelectBySubText(string subText)
        {
            getElement(highlight: false);
            try
            {
                if (_element.TagName == "div")
                {
                    var selectElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("select"));
                    if (selectElement != null)
                    {
                        _element = selectElement;
                    }
                    else
                    {
                        var ulElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("ul"));
                        if (ulElement != null)
                        {
                            _element = ulElement;
                        }
                    }
                }
                if (_element.TagName == "ul")
                {
                    // this is not a select statement.
                    var liCollection = _element.FindElements(OpenQA.Selenium.By.TagName("li"));
                    foreach (var li in liCollection)
                    {
                        if (li.Text.Contains(subText))
                        {
                            Highlight(li);
                            new Actions(Engine.WebDriver)
                                .MoveToElement(li, 0, 0)
                                .Click()
                                .Build()
                                .Perform();
                            return li.Text;
                        }
                    }
                    return "No Data Found";
                }
                else
                {
                    var selectElement = new SelectElement(_element);
                    foreach (var option in selectElement.Options.Where(option => option.Text.Contains(subText)))
                    {
                        var optionText = option.Text;
                        var action = new Actions(Engine.WebDriver);
                        var readyClick = action.MoveToElement(option).Click().Build();
                        readyClick.Perform();
                        return optionText;
                    }
                    selectElement.SelectByText(subText);
                    return subText;
                }
            }
            catch
            {
                _log.Error(subText + " substring could not be selected");
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// This will loop each option value until it finds the subText, then clicks on the Text.
        /// If it does not find the text on first pass, it will try SelectByText option.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string SelectByDisplayText(string text)
        {
            var inlineEditable = false;
            getElement(highlight: false, refreshElement: true);
            try
            {
                if (_element.TagName == "div")
                {
                    if (_element.GetAttribute("class").Contains("editable"))
                    {
                        inlineEditable = true;
                        var action = new Actions(Engine.WebDriver);
                        var readyClick = action.MoveToElement(_element).Click().Build();
                        readyClick.Perform();
                        Thread.Sleep(50); //for javascript
                        getElement(refreshElement: true);
                    }


                    var selectElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("select"));
                    if (selectElement != null)
                    {
                        _element = selectElement;
                    }
                    else
                    {
                        var ulElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("ul"));
                        if (ulElement != null)
                        {
                            _element = ulElement;
                        }
                    }
                }
                if (_element.TagName == "ul")
                {
                    // this is not a select statement.
                    var liCollection = Engine.FindChildElements(_element, OpenQA.Selenium.By.XPath("//li/a[contains(text(),'" + text + "')]"));
                    if (liCollection.Count > 0)
                    {
                        foreach (var li in liCollection)
                        {
                            if (li.Text.Equals(text))
                            {
                                new Actions(Engine.WebDriver)
                                    .MoveToElement(li, 3, 3)
                                    .Build()
                                    .Perform();
                                Thread.Sleep(40);
                                // double hover (scroll to) and single click.
                                new Actions(Engine.WebDriver)
                                    .MoveToElement(li, 3, 3)
                                    .Click()
                                    .Build()
                                    .Perform();
                                return text;
                            }
                        }
                    }
                    else
                    {
                        liCollection = _element.FindElements(OpenQA.Selenium.By.TagName("li"));
                        foreach (var li in liCollection)
                        {
                            if (li.Text.ToLower().Equals(text.ToLower()))
                            {
                                new Actions(Engine.WebDriver)
                                    .MoveToElement(li, 3, 3)
                                    .Build()
                                    .Perform();
                                Thread.Sleep(40);
                                // double hover (scroll to) and single click.

                                new Actions(Engine.WebDriver)
                                    .MoveToElement(li, 3, 3)
                                    .Click()
                                    .Build()
                                    .Perform();
                                return text;
                            }
                        }
                    }
                    _log.Error("No LI with text value: " + text);
                    return "No Data Found";
                }
                else
                {
                    try
                    {
                        Support.WaitForPageReadyState();
                        SelectElement select;
                        try
                        {
                            select = new SelectElement(_element);
                        }
                        catch (UnexpectedTagNameException unexpectedTagNameException)
                        {
                            _log.Error("The Dropdown was incorrectly done.", unexpectedTagNameException);
                            getElement(highlight: false, refreshElement: true);
                            _element.Click();
                            getElement(highlight: false, refreshElement: true);
                            select = new SelectElement(_element);
                        }

                        // for javascript data
                        Support.WaitForPageReadyState();
                        Support.WaitUntilChildElementIsPresent(_element,
                            OpenQA.Selenium.By.XPath(".//option[text()='" + text + "']"));

                        var option = Engine.FindChildElement(_element, OpenQA.Selenium.By.XPath(".//option[text()='" + text + "']"));
                        if (option.Displayed)
                        {
                            select.SelectByText(text);
                            if (Engine.Browser == SupportedBrowserType.Firefox)
                            {
                                _element.SendKeys(Keys.Enter);
                            }
                            Thread.Sleep(100);// for javascript
                            Support.WaitForPageReadyState();
                            if (inlineEditable)
                            {
                                //_element = Engine.FindElement(MyBy);
                                option = Engine.FindChildElement(_element, OpenQA.Selenium.By.XPath(".//option[text() ='" + text + "']"));
                                if (option != null && option.Displayed)
                                {
                                    try
                                    {
                                        var action = new Actions(Engine.WebDriver);
                                        var readyClick = action.MoveToElement(option).Click().Build();
                                        readyClick.Perform();
                                        GetParentElement().SendKeys(Keys.Tab);
                                        Thread.Sleep(100);// for javascript
                                        Support.WaitForPageReadyState();
                                    }
                                    catch (Exception)
                                    {
                                        Thread.Sleep(1000);
                                    }

                                }
                                
                            }

                        }
                        else
                        {
                            _log.Debug("Error in seeing the value in the Select. Problem is scrolling.");
                            Engine.Execute<object>(
                                "$sele = jQuery(arguments[0]); $sele.text('" + text + "')[0].scrollIntoView()", _element);
                            select.SelectByText(text);
                            Thread.Sleep(50);// for javascript
                            Support.WaitForPageReadyState();
                        }
                        //option.SendKeys(Keys.Tab);
                        _log.Debug(text + " selected");

                        return text;
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Element is not currently visible and so may not be interacted with"))
                            throw;
                        _log.Debug(ex.Message);
                        return "";
                        // scroll somehow to allow for the select value to be in play.
                    }

                    //Alternative way that should be looked at one day
                    /*var selectElement = new SelectElement(_element);
                    foreach (var option in selectElement.Options.Where(option => option.Text == text))
                    {
                        var optionText = option.Text;
                        option.Click();
                        return optionText;
                    }
                   // selectElement.SelectByText(text);
                    return text;
                     */
                }
            }
            catch
            {
                _log.Error(text + " could not be selected");
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        ///  This will select the element by .SelectByValue in Selenium  
        /// </summary>
        /// <param name="value"></param>
        public void SelectByValue(string value)
        {
            getElement(highlight: false);

            try
            {
                if (_element.TagName == "div")
                {
                    if (_element.GetAttribute("class").Contains("editable"))
                    {
                        var action = new Actions(Engine.WebDriver);
                        var readyClick = action.MoveToElement(_element).Click().Build();
                        readyClick.Perform();
                    }
                    var selectElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("select"));
                    if (selectElement != null)
                    {
                        _element = selectElement;
                    }
                    else
                    {
                        var ulElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("ul"));
                        if (ulElement != null)
                        {
                            _element = ulElement;
                        }
                    }
                }
                if (_element.TagName == "ul")
                {
                    // this is not a select statement.
                    var liCollection = _element.FindElements(OpenQA.Selenium.By.TagName("li"));
                    foreach (var li in liCollection)
                    {
                        if (li.GetAttribute("value").Equals(value))
                        {
                            Highlight(li);
                            new Actions(Engine.WebDriver)
                                .MoveToElement(li, 0, 0)
                                .Click()
                                .Build()
                                .Perform();
                            _log.Debug(value + " selected");
                            break;
                        }
                    }
                }
                else if (_element.TagName == "select")
                {
                    try
                    {
                        var select = new SelectElement(_element);
                        var option = Engine.FindChildElement(_element, OpenQA.Selenium.By.XPath("//option[@value ='" + value + "']"));
                        if (option.Displayed)
                        {
                            select.SelectByValue(value);
                        }
                        else
                        {
                            _log.Debug("Error in seeing the value in the Select. Problem is scrolling.");
                            Engine.Execute<object>(
                                "$sele = jQuery(arguments[0]); $sele.val('" + value + "')[0].scrollIntoView()", _element);
                            select.SelectByValue(value);
                        }
                        //option.SendKeys(Keys.Tab);
                        //Engine.FindElement(OpenQA.Selenium.By.TagName("body")).Click();
                        _log.Debug(value + " selected");
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("Element is not currently visible and so may not be interacted with"))
                            throw;
                        _log.Debug(ex.Message);
                        // scroll somehow to allow for the select value to be in play.
                    }
                }
                else
                {
                    throw new Exception("This is the wrong kind of tag for selecting value : " + _element.TagName);
                }
            }
            catch (StaleElementReferenceException)
            {
                _element = Engine.WebDriver.FindElement(MyBy);
                SelectByValue(value);
            }
            catch
            {
                _log.Error(value + " could not be selected");
                Support.ScreenShot();
                throw;
            }
        }


        /// <summary>
        /// Summary: Gets the selected item within the select element.
        /// If more than one item is selected this will return the first item.
        /// </summary>
        public IWebElement SelectedElement
        {
            get
            {
                try
                {
                    getElement(highlight: false);
                    switch (_element.TagName)
                    {
                        case "select":
                        {
                            var select = new SelectElement(_element);
                            return @select.SelectedOption;
                        }
                        case "li":
                        {
                            var selectedElement = _element.FindElement(OpenQA.Selenium.By.ClassName("selected"));
                            return selectedElement;
                        }
                        case "ul":
                        {
                            var selectedElement = _element.FindElement(OpenQA.Selenium.By.ClassName("selected"));
                            return selectedElement;
                        }
                        case "div":
                        {
                            if (_element.GetAttribute("class").Contains("editable"))
                            {
                                var action = new Actions(Engine.WebDriver);
                                var readyClick = action.MoveToElement(_element).Click().Build();
                                readyClick.Perform();
                                getElement(highlight: true);
                            }
                            var selectElement = Engine.FindChildElement(_element, OpenQA.Selenium.By.TagName("select"));
                            if (selectElement != null)
                            {
                                _element = selectElement;
                                var select = new SelectElement(_element);
                                return @select.SelectedOption;
                            }
                        }
                        break;
                    }
                    _log.Debug("SelectedOption: TagName unknown");
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                    return SelectedElement;
                }
                catch (Exception ex)
                {
                    _log.Error("Could not see selected for by: " + MyBy, ex);
                    Support.ScreenShot();
                    throw;
                }
            }
        }

        /// <summary>
        /// Summary: Gets the selected item within the select element.
        /// If more than one item is selected this will return the first item.
        /// </summary>
        public SelectElement SelectElement
        {
            get
            {
                try
                {
                    getElement(highlight: false, refreshElement: true);
                    if (_element.TagName.Equals("select"))
                    {
                        var select = new SelectElement(_element);
                        return @select;
                    }
                    else if (_element.TagName.Equals("div") && _element.GetAttribute("class").Contains("editable"))
                    {
                        var action = new Actions(Engine.WebDriver);
                        var readyClick = action.MoveToElement(_element).Click().Build();
                        readyClick.Perform();
                        getElement(highlight: false);

                        var select = new SelectElement(_element.FindElement(By.TagName("select")));
                        return @select;
                    }
                    _log.Debug("Select Element: TagName unknown");
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    var select = new SelectElement(_element);
                    return @select;
                }
                catch (Exception ex)
                {
                    _log.Error("Could not see selectElement for by: " + MyBy, ex);
                    Support.ScreenShot();
                    throw;
                }
            }
        }


        /// <summary>
        /// Summary: Gets the selected item within the select element.
        /// If more than one item is selected this will return the first item.
        /// </summary>
        public IWebElement SelectedOption
        {
            get
            {
                return SelectedElement;
            }
        }

        /// <summary>
        /// Summary: Gets the selected item within the select element, then the html VALUE 
        /// If more than one item is selected this will return the first item.
        /// </summary>
        public string SelectedTextDisplayed
        {
            get
            {
                getElement(highlight: true);
                if (_element != null &&
                    _element.Displayed &&
                    _element.GetAttribute("class").Contains("editable"))
                {
                    return _element.Text;
                }

                return SelectedElement.Text; 
            }
        }
        /// <summary>
        /// Summary: Gets the selected item within the select element, then the html VALUE 
        /// If more than one item is selected this will return the first item.
        /// </summary>
        public string SelectedValue
        {
            get
            {
                return SelectedElement.GetAttribute("value");
            }
        }

        /// <summary>
        /// This will select the object, and then run a javascript command to blur.
        /// </summary>
        /// <param name="value"></param>
        public void SelectByValueWithBlur(string value)
        {
            getElement(highlight: false);
            try
            {
                var select = new SelectElement(_element);
                select.SelectByValue(value);
                _log.Debug(value + " selected");

                Engine.Execute<object>(Engine.WebDriver, "return arguments[0].blur()", _element);
                Engine.FindElement(OpenQA.Selenium.By.TagName("body")).SendKeys(Keys.Tab);
            }
            catch(Exception ex)
            {
                _log.Error(value + " could not be selected", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Simulates typing text into the element.
        /// </summary>
        /// <param name="text"></param>
        public void SendKeys(string text)
        {
            Support.WaitForPageReadyState();
            getElement();
            try
            {
                if (Engine.Browser == SupportedBrowserType.Ie || Engine.Browser == SupportedBrowserType.Edge)
                {
                    if (!_element.Displayed)
                    {
                        var opacity = _element.GetCssValue("opacity");
                        if (string.IsNullOrEmpty(opacity) || opacity == "0")
                        {
                            SetAttribute("style", "opacity: 1");
                        }
                        getElement();
                    }
                }
                if (text == "TAB")
                {
                   _element.SendKeys(Keys.Tab);
                    return;
                }
                if (text == "ESC" || text == "ESCAPE")
                {
                    _element.SendKeys(Keys.Escape);
                    return;
                }
                if (text == "SPACE")
                {
                    _element.SendKeys(Keys.Space);
                    return;
                }
                if (text == "ENTER")
                {
                    _element.SendKeys(Keys.Enter);
                    return;
                }
                _log.Debug("Typing " + text + " in " + selector);
                _element.SendKeys(text);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to type " + text + " in " + MyBy + " : ", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Simulates typing text into the element.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clearFirst"></param>
        public void SendKeys(string text, bool clearFirst)
        {
            using (PerformanceTimer.Start(
                ts => PerformanceTimer.LogTimeResult("SendKeys " + selector, ts)))
            {
                getElement();
                try
                {
                    if (clearFirst) _element.Clear();
                    if (text == "BLANK") return;
                    _element.SendKeys(text);
                    _log.Debug("Typing " + text + " in " + selector);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Element not found in the cache")) 
                    {
                        getElement();
                        try
                        {
                            if (clearFirst) _element.Clear();
                            _element.SendKeys(text);
                            _log.Debug("Typing " + text + " in " + selector, ex);
                        }
                        catch (Exception ex2)
                        {
                            if (ex.Message.Contains("Element not found in the cache")) 
                            {
                            }
                            _log.Error("Unable to type " + text + " in " + selector, ex2);
                            Support.ScreenShot();
                            throw;
                        }
                    }
                    _log.Error("Unable to type " + text + " in " + selector, ex);
                    Support.ScreenShot();
                    throw;
                }
            }
        }

        /// <summary>
        /// IWebElement s have a handy GetAttribute function. This is useful, for example, for retrieving an input's value (which is not the same as an input's text). So why is there no SetAttribute function? The reason is simply that the creators of Selenium have strived to create a tool that simulates a user interacting with the web. A human user would not normally be able to modify the underlying attributes of an .
        /// Regardless, setting an element's attributes can be essential for working around some Selenium quirks. For example, masked input fields don't play well with SendKeys. In this case, we have to directly set the value of the field.
        /// The only downside is the web driver must have javascript enabled.
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="value"></param>
        public void SetAttribute(string attributeName, string value)
        {
            getElement(highlight: false);
            //var wrappedElement = _element as IWrapsDriver;
            //if (wrappedElement == null)
            //    throw new ArgumentException("element", "Element must wrap a web driver");

            //var driver = wrappedElement.WrappedDriver;
            //var javascript = driver as IJavaScriptExecutor;
            //if (javascript == null)
            //    throw new ArgumentException("Element must wrap a web driver that supports javascript execution");

            Engine.Execute<object>("arguments[0].setAttribute(arguments[1], arguments[2])", _element, attributeName, value);
        }


        /// <summary>
        /// IWebElement s have a handy GetAttribute function. This is useful, for example, for retrieving an input's value (which is not the same as an input's text). So why is there no SetAttribute function? The reason is simply that the creators of Selenium have strived to create a tool that simulates a user interacting with the web. A human user would not normally be able to modify the underlying attributes of an .
        /// Regardless, setting an element's attributes can be essential for working around some Selenium quirks. For example, masked input fields don't play well with SendKeys. In this case, we have to directly set the value of the field.
        /// The only downside is the web driver must have javascript enabled.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attributeName"></param>
        /// <param name="value"></param>
        public void SetAttribute(IWebElement element, string attributeName, string value)
        {
            //var wrappedElement = _element as IWrapsDriver;
            //if (wrappedElement == null)
            //    throw new ArgumentException("element", "Element must wrap a web driver");

            //var driver = wrappedElement.WrappedDriver;
            //var javascript = driver as IJavaScriptExecutor;
            //if (javascript == null)
            //    throw new ArgumentException("Element must wrap a web driver that supports javascript execution");

            Engine.Execute<object>("arguments[0].setAttribute(arguments[1], arguments[2])", element, attributeName, value);
        }

        /// <summary>
        /// Set / override the default timeout that is used by the Wait methods.
        /// </summary>
        /// <param name="x"></param>
        public static void SetTimeout(int x)
        {
            DefaultTimeout = x;
        }

        /// <summary>
        /// Set the z-index of the Element.  This might be a good way to pull an element to the top.
        /// </summary>
        /// <param name="index"></param>
        public void SetZindex(int index = 999)
        {
            getElement(highlight: false);
            SetAttribute(_element, "style", "z-index:" + index.ToString());
        }

        /// <summary>
        /// This will return the size of the element.
        /// </summary>
        public Size Size
        {
            get
            {
                getElement(highlight: false);
                return Element.Size;
            }
        }

        /// <summary>
        /// This will perform Selenium's Submit function
        /// If this current element is a form, or an element within a form, then this will be submitted to the web server. If this causes the current page to change, then this method will block until the new page is loaded.
        /// </summary>
        public void Submit()
        {
            getElement(highlight: false);
            try
            {
                _element.Submit();
                _log.Debug(MyBy + " submitted");
            }
            catch (Exception ex)
            {
                _log.Error(MyBy + " failed to submit", ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// using the Employee Picker Supapicka select the right person.
        /// </summary>
        /// <param name="name"></param>
        public void Supapicka(string name)
        {
            Support.WaitForPageReadyState();
            getElement(refreshElement:true);
            try
            {
                var startButton = _element;
                if (_element.TagName != "a")
                {
                    startButton = Engine.FindChildElement(_element,
                        OpenQA.Selenium.By.XPath(".//button[contains(text(),'Select') and contains(@class,'ui-button') and not(ancestor::*[contains(@style,'display:none')]) and not(ancestor::*[contains(@style,'display: none')])]"));
                }

                if (startButton == null)
                    throw new Exception("No Button found to start the select process.");

                var action = new Actions(Engine.WebDriver);
                var readyClick = action.MoveToElement(startButton).Click().Build();
                readyClick.Perform();

                var searchButton = Engine.FindChildElement(_element, OpenQA.Selenium.By.ClassName("js-select-existing"));
                if (searchButton != null)
                    searchButton.Click();

                // new iframe loads.
                Engine.SwitchOutToDefaultContent();
                Engine.SwitchToiFrameWithAttribute("src", "/agent/choose/Employee?CorrelationId=");

                // find the grid.
                var grid = new Control(How.XPath, "//table[contains(@id,'gridContainer_')]");

                var by = OpenQA.Selenium.By.XPath($".//td[contains(text(),'{name}')]");
                var cell = grid.GetChildControl(by);

                cell.DoubleClickAt(3, 3);
                _log.Debug(name + " double clicked");
                cell.WaitForElementToNotBeVisible(16);
                if (Engine.FindElement(By.TagName("table")) == null)
                    return;  // work is done.

                Support.WaitForPageReadyState();
                if (!cell.IsStaleReference && cell.IsDisplayed)  // for when double click doesn't work.
                {
                    var select = new Control(How.Name, "selectButton");
                    select.ClickAt(5, 5);

                    Thread.Sleep(100);
                    if (select.IsDisplayed)
                    {
                        _log.Error("selectButton did not click correctly, element is still displayed.");
                        var rowControl = cell.GetParentControl();
                        if (!rowControl.HasClass("ui-state-highlight"))
                        {
                            rowControl.AddClass("ui-state-highlight");
                        }
                        rowControl.DoubleClick();

                        if (select.IsDisplayed)
                        {
                            select.ClickAt(5, 5);
                            if (select.IsDisplayed)
                                _log.Error(
                                    "selectButton did not click correctly second time, element is still displayed.");
                        }
                    }
                    _log.Debug("selectButton clicked");
                }
                // wait for page transitions
                //Thread.Sleep(1000);
                Support.WaitForPageReadyState(TimeSpan.FromSeconds(16));
            }
            catch (Exception ex)
            {
                _log.Error(MyBy + " could not be clicked " , ex);
                Support.ScreenShot();
                throw;
            }
        }

        /// <summary>
        /// Uses the By/How to locate the iFrame Element, and then switches to that iframe.
        /// this is not meant to work with just any kind of element.
        /// </summary>
        /// <returns></returns>
        public IWebDriver SwitchToIframe()
        {
            Engine.SwitchOutofiFrames();
            getElement(highlight: false);
            var iFrame = _element;
            if (iFrame == null)
            {
                Engine.SwitchOutofiFrames();
                iFrame = Engine.FindElement(MyBy);
            }
            if (iFrame == null)
                throw new Exception("no iframe found for " + MyBy);
            _log.Debug("Switching to iframe: " + MyBy);
            return Engine.WebDriver.SwitchTo().Frame(iFrame);
        }

        /// <summary>
        /// The OpenQA.Selenium.IWebElement.TagName property returns the tag name of the element, not the value of the name attribute. For example, it will return "input" for an element specified by the HTML markup <input name="foo" />.
        /// </summary>
        public string TagName
        {
            get
            {
                getElement();
                return _element.TagName;
            }
        }

        /// <summary>
        /// Gets the innerText of this element, without any leading or trailing whitespace, and with other whitespace collapsed.
        /// </summary>
        public string Text
        {
            get
            {
                try
                {
                    Support.WaitForPageReadyState();
                    getElement(highlight: true, refreshElement: true);
                    return _element.Text.Trim();
                }
                catch (StaleElementReferenceException)
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                    return _element.Text.Trim();
                }
            }
        }

        /// <summary>
        /// Gets the innerText of this element, then uses regex to get only Digits from the text.
        /// </summary>
        public string TextDigitsOnly
        {
            get
            {
                try
                {
                    Support.WaitForPageReadyState();
                    getElement(highlight: false);

                    var regexObj = new Regex(@"[^\d]");
                    var digitOnly = regexObj.Replace(_element.Text, "");
                    _log.Debug("Digits found: " + digitOnly);
                    return digitOnly;
                }
                catch (StaleElementReferenceException)
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                    var regexObj = new Regex(@"[^\d]");
                    return regexObj.Replace(_element.Text, "");
                }
            }
        }

        /// <summary>
        /// Gets the innerText of this element, without any leading or trailing whitespace, and with other whitespace collapsed.
        /// </summary>
        public string Title
        {
            get
            {
                try
                {
                    getElement(highlight: false);
                    return _element.GetAttribute("title");
                }
                catch (StaleElementReferenceException)
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                    return _element.GetAttribute("title");
                }

            }
        }

        /// <summary>
        /// Gets the Value of the element
        /// example: <input id="login-button" type="submit" value="Login"/>  value returned is Login
        /// </summary>
        public string Value
        {
            get
            {
                try
                {
                    Support.WaitForPageReadyState();
                    getElement(highlight: false);
                    return _element.GetAttribute("value");
                }
                catch (StaleElementReferenceException)
                {
                    _element = Engine.WebDriver.FindElement(MyBy);
                    return _element.GetAttribute("value");
                }
            }
        }

        /// <summary>
        /// Wait for Element to have class
        /// </summary>
        public void WaitForElementClass(string classString, int seconds = 8)
        {
            try
            {
                Support.WaitForPageReadyState();
                var element = Support.WaitUntilElementHasClass(MyBy, classString, TimeSpan.FromSeconds(seconds));
            }
            catch (Exception ex)
            {
                _log.Error("Support.WaitUntilElementIsNotVisible", ex);
            }

        }

        /// <summary>
        /// Wait for Element to be not Visible/present
        /// </summary>
        /// <param name="seconds"></param>
        public void WaitForElementToNotBeVisible(int seconds = 8)
        {
            try
            {
                Support.WaitUntilElementIsNotVisible(MyBy, TimeSpan.FromSeconds(seconds));
            }
            catch (Exception ex)
            {
                // ignore
                _log.Error("Support.WaitUntilElementIsNotVisible", ex);
            }

        }

        /// <summary>
        /// Wait for Element to be Enabled/Present
        /// </summary>
        public IWebElement WaitForElementPresent(int seconds = 8)
        {
            var element = Support.WaitUntilElementIsPresent(MyBy, TimeSpan.FromSeconds(seconds));
             return element;
        }

        /// <summary>
        /// Wait for Element to be Visible
        /// </summary>
        public bool WaitForElementVisible(bool hoverAction = true, int seconds = 8)
        {
            try
            {
                Support.WaitForPageReadyState();
                var element = Support.WaitUntilElementIsVisible(MyBy, TimeSpan.FromSeconds(seconds));
                if (element == null)
                    return false;

                 // hover over
                if (hoverAction)
                {
                    var action = new Actions(Engine.WebDriver);
                    action.MoveToElement(element).Build().Perform();
                }

                return IsEnabled;
            }
            catch (Exception)
            {
                //ignore
                return false;
            }
        }

        /// <summary>
        /// Wait for Element to have text
        /// </summary>
        public bool WaitForElementText(string text = "", bool hoverAction = true, int waitForSeconds = 10)
        {
            try
            {
                Support.WaitForPageReadyState();
                var element = Support.WaitUntilElementHasText(MyBy, text, TimeSpan.FromSeconds(waitForSeconds));
                if (element == null)
                    return false;

                // hover over
                if (hoverAction)
                {
                    var action = new Actions(Engine.WebDriver);
                    action.MoveToElement(element).Build().Perform();
                }
                return true;
            }
            catch (Exception)
            {
                // ignore
                return false;
            }
        }

        /// <summary>
        /// Wait for Element to not have text
        /// </summary>
        public bool WaitForElementToNotHaveText(string text = "", int waitForSeconds = 10)
        {
            try
            {
                Support.WaitForPageReadyState();
                var element = Support.WaitUntilElementDoesNotHaveText(MyBy, text, TimeSpan.FromSeconds(waitForSeconds));
                if (element == null)
                    return false;
                return true;
            }
            catch (Exception)
            {
                // ignore
                return false;
            }
        }

        /// <summary>
        /// Generate an XPath of the element.  This is useful in Debugging.
        /// </summary>
        public string XPath
        {
            get
            {
                getElement(highlight: false);
                var script = xpathbuilder();
                var command = "return getElementXPath(arguments[0]);";
                var xpath = Engine.Execute<string>(Engine.WebDriver, script + " " + command, _element);
                _log.Debug(SelectorUsed + " has xpath = " + xpath);
                return xpath;
            }
        }

        static string xpathbuilder()
        {
            var sb = new StringBuilder();
            sb.Append("  var getElementTreeXPath = function(element) { ");
            sb.Append("  var paths = []; ");
            sb.Append("  ");
            sb.Append("  for (; element && element.nodeType == 1; element = element.parentNode)  { ");
            sb.Append("       var index = 0; ");
            sb.Append("       if (element && element.id) { ");
            sb.Append(@"           paths.splice(0, 0, '/*[@id=""' + element.id + '""]'); ");
            sb.Append("           break; ");
            sb.Append("       } ");
            sb.Append(
                "       for (var sibling = element.previousSibling; sibling; sibling = sibling.previousSibling) { ");
            sb.Append("          if (sibling.nodeType == Node.DOCUMENT_TYPE_NODE) ");
            sb.Append("                 continue; ");
            sb.Append("          if (sibling.nodeName == element.nodeName) ");
            sb.Append("                   ++index; ");
            sb.Append("          } ");
            sb.Append("          var tagName = element.nodeName.toLowerCase(); ");
            sb.Append("          var pathIndex = (index ? '[' + (index+1) + ']' : ''); ");
            sb.Append("           paths.splice(0, 0, tagName + pathIndex); ");
            sb.Append("       } ");
            sb.Append("     return paths.length ? '/' + paths.join('/') : null; ");
            sb.Append("  }; ");
            sb.Append(" var getElementXPath = function(element) { ");
            sb.Append(" if (element && element.id) ");
            sb.Append(@"  return '//*[@id=""' + element.id + '""]'; ");
            sb.Append(" else ");
            sb.Append("   return getElementTreeXPath(element); ");
            sb.Append(" }; ");
            return sb.ToString();
        }

        #endregion
    }

    public static class PageObject
    {
        public static DateTime ToUtcDateTimeFromLocal(this string input)
        {
            DateTime localDateTime = DateTime.Parse(input);
            DateTime utcDateTime = localDateTime.ToUniversalTime();
            return utcDateTime;
        }
        public static Dictionary<string, Control> Controls;

        public static void PopulateControls()
        {
        }

        /// <summary>
        /// instructions for their usage: http://www.slideshare.net/LivePersonDev/selenium-webdriver-element-locators
        /// </summary>
        public enum How
        {
            ClassName,
            CssSelector,
            DataSupapicka,
            Id,
            LinkText,
            Name,
            PartialLinkText,
            TagName,
            XPath,
            // ReSharper disable once InconsistentNaming
            jQuery,
            Custom,
            HtmlTag,
            CssValue,
            Src
        };
    }
}