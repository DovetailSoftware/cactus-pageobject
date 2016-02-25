using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Cactus.Drivers
{
    public static class NunitExtensions
    {

        /// <summary>
        /// Assert All using Actions and Objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <param name="test"></param>
        public static void AssertAll<T>(this IEnumerable<T> objects, Action<T> test)
        {
            int total = 0;
            int passed = 0;
            int failed = 0;
            int inconclusive = 0;
            var sb = new StringBuilder();
            foreach (var obj in objects)
            {
                total++;
                try
                {
                    test(obj);
                    passed++;
                }
                catch (InconclusiveException assertion)
                {
                    inconclusive++;
                    string message = $"INCONCLUSIVE: {obj.ToString()}: {assertion.Message}";
                    Console.WriteLine(message);
                    sb.AppendLine(message);
                }
                catch (AssertionException assertion)
                {
                    failed++;
                    string message = $"FAILED: {obj.ToString()}: {assertion.Message}";
                    Console.WriteLine(message);
                    sb.AppendLine(message);
                }
            }

            if (passed != total)
            {
                string details = sb.ToString();
                string message = $"{passed} of {total} tests passed; {inconclusive} inconclusive\n{details}";


                if (failed == 0)
                {
                    Assert.Inconclusive(message);
                }
                else
                {
                    Assert.AreEqual(total, passed, message);
                }
            }
        }

        /// <summary>
        /// This is a trial of comparing 2 datatables for test cases.
        /// </summary>
        /// <param name="dataTableShouldBe"></param>
        /// <param name="dataTableFromTestCase"></param>
        /// <returns></returns>
        public static DataTable CompareDataTable(this DataTable dataTableFromTestCase, DataTable dataTableShouldBe)
        {
            bool changeDt = false;
            List<string> changeTypeColNames = new List<string>();
            foreach (DataColumn col in dataTableShouldBe.Columns.Cast<DataColumn>().Where(col => col.DataType != System.Type.GetType("System.String")))
            {
                Console.WriteLine(col.ColumnName + " " + col.DataType.GetType());
                changeDt = true;
                changeTypeColNames.Add(col.ColumnName);
            }

            DataTable dataTableShouldBeClone = new DataTable();
            if (changeDt)
            {
                foreach (var name in changeTypeColNames)
                {
                    //Console.WriteLine(col.ColumnName + " " + col.DataType.GetType());
                    dataTableShouldBeClone = changeColumnDataType(dataTableShouldBeClone, columnname: name,
                        newtype: System.Type.GetType("System.String"));
                }
            }
            else
            {
                dataTableShouldBeClone = dataTableShouldBe.Clone();
            }
            changeTypeColNames.Clear();

            foreach (DataRow dr in dataTableShouldBe.Rows)
            {
                dataTableShouldBeClone.ImportRow(dr);
            }

            changeDt = false;
            foreach (DataColumn col in dataTableFromTestCase.Columns.Cast<DataColumn>().Where(col => col.DataType != System.Type.GetType("System.String")))
            {
                changeDt = true;
                changeTypeColNames.Add(col.ColumnName);
            }
            DataTable dataTableFromTestCaseClone = new DataTable();

            if (changeDt)
            {
                foreach (var name in changeTypeColNames)
                {
                    //Console.WriteLine(col.ColumnName + " " + col.DataType.GetType());
                    dataTableFromTestCaseClone = changeColumnDataType(dataTableFromTestCaseClone, columnname: name,
                        newtype: System.Type.GetType("System.String"));
                }
            }
            else
            {
                dataTableFromTestCaseClone = dataTableFromTestCase.Clone();
            }
            foreach (DataRow dr in dataTableFromTestCase.Rows)
            {
                dataTableFromTestCaseClone.ImportRow(dr);
            }

            var resultDataTable = new DataTable();
            resultDataTable = dataTableShouldBeClone.Clone();
            resultDataTable.Columns.Add(new DataColumn("Result", System.Type.GetType("System.String")) { DefaultValue = "unknown" });

            if (dataTableFromTestCaseClone.Rows.Count == 0)
                throw new Exception("Data Table did not gen right.");
            if (dataTableShouldBeClone.Rows.Count == 0)
                throw new Exception("Data Table did not gen right.");

            dataTableFromTestCaseClone.TableName = dataTableShouldBeClone.TableName;
            // make sure there are no Nulls in these tables. Null doesn't equal Empty.
            foreach (DataRow row in dataTableFromTestCaseClone.Rows)
            {
                foreach (var column in dataTableFromTestCaseClone.Columns.Cast<DataColumn>().Where(column => row[column] == null))
                {
                    row[column] = string.Empty;
                }
            }

            foreach (DataRow row in dataTableShouldBeClone.Rows)
            {
                foreach (var column in dataTableShouldBeClone.Columns.Cast<DataColumn>().Where(column => row[column] == null))
                {
                    row[column] = string.Empty;
                }
            }


            var intersectionRows = dataTableShouldBeClone.AsEnumerable().Intersect(dataTableFromTestCaseClone.AsEnumerable(), DataRowComparer.Default);
            foreach (DataRow row in intersectionRows)
            {
                resultDataTable.ImportRow(row);
            }
            foreach (var row in resultDataTable.Rows.Cast<DataRow>().Where(row => row["Result"].ToString() == "unknown"))
            {
                row["Result"] = "Passed";
            }


            // Find the rows that are in the first  
            // table but not the second. 
            var nonIntersectionRowsExtra = dataTableShouldBeClone.AsEnumerable().Except(dataTableFromTestCaseClone.AsEnumerable(),
                DataRowComparer.Default);

            foreach (DataRow row in nonIntersectionRowsExtra)
            {
                resultDataTable.ImportRow(row);
            }
            foreach (var row in resultDataTable.Rows.Cast<DataRow>().Where(row => row["Result"].ToString() == "unknown"))
            {
                row["Result"] = "Missing";
            }


            // Find the rows that are in the second  
            // table but not the first. 
            var nonIntersectionRowsMissing = dataTableFromTestCaseClone.AsEnumerable().Except(dataTableShouldBeClone.AsEnumerable(),
                DataRowComparer.Default);

            foreach (DataRow row in nonIntersectionRowsMissing)
            {
                resultDataTable.ImportRow(row);
            }
            foreach (var row in resultDataTable.Rows.Cast<DataRow>().Where(row => row["Result"].ToString() == "unknown"))
            {
                row["Result"] = "Extra";
            }

            var count = resultDataTable.Rows.Count;

            if (count > 0)
            {
                Console.WriteLine("-___--___--___--___--");
                foreach (DataColumn col in resultDataTable.Columns)
                {
                    Console.Write(col.ColumnName + " ");
                }
                foreach (DataRow row in resultDataTable.Rows)
                {
                    Console.WriteLine();
                    for (int x = 0; x < resultDataTable.Columns.Count; x++)
                    {
                        Console.Write(row[x].ToString() + " ");
                    }
                }
                Console.WriteLine();
                Console.WriteLine("-___--___--___--___--");
            }

            if (resultDataTable.Rows.Cast<DataRow>().Select(row => row["Result"].ToString() != "Passed").Any())
            {   // this will be a fail, so take a screenshot.
                if (Engine.WebDriver != null)
                {
                    Support.ScreenShot();
                }
            }

            resultDataTable.AsEnumerable().AssertAll((DataRow row) => Assert.That(row["Result"].ToString() == "Passed"
                , "Row {0} is not Passing/Equal Data, {1}", row[0].ToString(), row["Result"].ToString()));

            return resultDataTable;

        }

        static DataTable changeColumnDataType(DataTable table, string columnname, Type newtype)
        {
            if (table.Columns.Contains(columnname) == false)
                return table;

            DataColumn column = table.Columns[columnname];
            if (column.DataType == newtype)
                return table;

            try
            {
                DataColumn newcolumn = new DataColumn("temporary", newtype);
                table.Columns.Add(newcolumn);
                newcolumn.SetOrdinal(table.Columns.IndexOf(columnname));
                foreach (DataRow row in table.Rows)
                {
                    try
                    {
                        row["temporary"] = Convert.ChangeType(row[columnname], newtype);
                    }
                    catch
                    {
                    }
                }
                table.Columns.Remove(columnname);
                newcolumn.ColumnName = columnname;
            }
            catch (Exception)
            {
                return table;
            }

            return table;
        }

        /// <summary>
        /// Prototype of Comparing lists.  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataListFromTestCase"></param>
        /// <param name="dataListShouldBe"></param>
        /// <returns></returns>
        public static bool CompareLists<T>(this IEnumerable<T> dataListFromTestCase, IEnumerable<T> dataListShouldBe)
        {
            // 1
            // Require that the counts are equal
            if (dataListFromTestCase.Count() != dataListShouldBe.Count())
            {
                Support.ScreenShot();
                Assert.Fail(string.Format("The list count should have been {0}, but was {1}{2}Data from Test Was: [{3}]{2}Should have been:[{4}]",
                    dataListShouldBe.Count(),
                    dataListFromTestCase.Count(),
                    Environment.NewLine,
                    string.Join(",", dataListFromTestCase.Select(i => i.ToString()).ToArray()),
                    string.Join(",", dataListShouldBe.Select(i => i.ToString()).ToArray())
                    ));
            }
            // 2
            // Initialize new Dictionary of the type
            Dictionary<T, int> d = new Dictionary<T, int>();
            // 3
            // Add each key's frequency from collection A to the Dictionary
            foreach (T item in dataListFromTestCase)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    d[item] = c + 1;
                }
                else
                {
                    d.Add(item, 1);
                }
            }
            // 4
            // Add each key's frequency from collection B to the Dictionary
            // Return early if we detect a mismatch
            foreach (T item in dataListShouldBe)
            {
                int c;
                if (d.TryGetValue(item, out c))
                {
                    if (c == 0)
                    {
                        return false;
                    }
                    else
                    {
                        d[item] = c - 1;
                    }
                }
                else
                {
                    // Not in dictionary
                    return false;
                }
            }
            // 5
            // Verify that all frequencies are zero

            foreach (int v in d.Values)
            {
                if (v != 0)
                {
                    if (Engine.WebDriver != null)
                    {
                        Support.ScreenShot();
                    }
                    Assert.Fail("The list count should have been {0}, but was {1}{2}Data from Test Was: [{3}]",
                                        dataListShouldBe.Count(),
                                        dataListFromTestCase.Count(),
                                        Environment.NewLine,
                                        d);
                }
            }

            d.Values.AsEnumerable().AssertAll((v) => Assert.That(v == 0, "Failed for List row "));

            // 6
            // We know the collections are equal
            return true; // Assert.Pass
        }

        /// <summary>
        /// Assert that the Current URL Contains the full URL of the URL expected.
        /// </summary>
        /// <param name="expectedFullurl"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string AssertCurrentUrlEquals(string expectedFullurl, string message = "")
        {
            Support.WaitForUrlToContain(expectedFullurl, TimeSpan.FromMilliseconds(5000));

            var currentUrl = Engine.GetCurrentUrl;
            StringAssert.AreEqualIgnoringCase(expectedFullurl, currentUrl, message: "FAIL: " +
                string.Format("Test line was expecting url : {0} {1} {0} and was :{2}", Environment.NewLine,
                    expectedFullurl, currentUrl) + Environment.NewLine + message);

            Engine.Log.Info("PASS: " +
                string.Format("Test line was expecting url : {0} {1} {0} and was :{2}", Environment.NewLine,
                    expectedFullurl, currentUrl));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return currentUrl;
        }

        /// <summary>
        /// Assert that the Current URL Contains the part of the URL expected.
        /// </summary>
        /// <param name="expectedPartialUrl"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string AssertCurrentUrlContains(string expectedPartialUrl, string message = "")
        {

            Support.WaitForUrlToContain(expectedPartialUrl, TimeSpan.FromSeconds(16));

            var currentUrl = Engine.GetCurrentUrl;
            StringAssert.Contains(expectedPartialUrl.ToLower(), currentUrl.ToLower(), message: "FAIL: " +
                string.Format("Test line was expecting url : {0} {1} {0} and was :{2}", Environment.NewLine,
                    expectedPartialUrl.ToLower(), currentUrl.ToLower()) + Environment.NewLine + message);

            Engine.Log.Info("PASS: " +
                string.Format("Test line was expecting url : {0} {1} {0} and was :{2}", Environment.NewLine,
                    expectedPartialUrl.ToLower(), currentUrl.ToLower()));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
            return currentUrl;
        }

        /// <summary>
        /// Asserts that the given element Collection, has within at least 1 element, the expected text.
        /// </summary>
        /// <param name="elementCollection"></param>
        /// <param name="exceptedText"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IWebElement AssertContainsText(this IEnumerable<IWebElement> elementCollection, string exceptedText = "", string message = "")
        {
            bool pass = false;
            IWebElement returnElement = null;
            var stringConcatinate = "";  // used for error message.
            foreach (var element in elementCollection)
            {
                if (element != null && element.Text != null)
                {
                    if (element.Text.Contains(exceptedText))
                    {
                        pass = true;
                        returnElement = element;
                        break;
                    }
                    stringConcatinate += element.Text;
                }
            }

            if (!pass)
            {
                Assert.Fail(message: "FAIL: " +
                                     string.Format(
                                         "Test line was expecting text in the Collection : {0} {1} {0} and was :{2}",
                                         Environment.NewLine,
                                         exceptedText, stringConcatinate) + Environment.NewLine + message);
            }
            else
            {
                Engine.Log.Info("PASS: " +
                    string.Format("Test line was expecting text in the Collection : {0} {1} {0} and is :{2}", Environment.NewLine,
                    exceptedText, returnElement.Text));

                new TestLineStatusWithEvent().Status(TestStatus.Passed);
                return returnElement;
            }
            return null;
        }

        /// <summary>
        /// Just to make our testing easier, all ourselves to use the real SequenceEquals
        /// call from LINQ to Objects.
        /// </summary>
        public static void AssertSequenceEqual<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            Assert.IsTrue(actual.SequenceEqual(expected), "FAIL: " + "Expected: " +
                    ",".InsertBetween(expected.Select(x => Convert.ToString(x))) + "; " + Environment.NewLine + "was: " +
                    ",".InsertBetween(actual.Select(x => Convert.ToString(x))));

            Engine.Log.Info("PASS: " + "Expected: " +
                    ",".InsertBetween(expected.Select(x => Convert.ToString(x))) + "; " + Environment.NewLine + "was: " +
                    ",".InsertBetween(actual.Select(x => Convert.ToString(x))));

            new TestLineStatusWithEvent().Status(TestStatus.Passed);
        }

        /// <summary>
        /// Make testing even easier - a params array makes for readable tests :)
        /// The sequence is evaluated exactly once.
        /// </summary>
        public static void AssertSequenceEqual<T>(this IEnumerable<T> actual, params T[] expected)
        {
            if (!actual.Any())
            {
                Assert.Fail("Expected: " +
                    ",".InsertBetween(expected.Select(x => Convert.ToString(x))) + "; was: " +
                    "Empty Array");
            }

            // Working with a copy means we can look over it more than once.
            // We're safe to do that with the array anyway.

            var copy = actual.ToList();
            var result = copy.SequenceEqual(expected);
            // Looks nicer than Assert.IsTrue or Assert.That, unfortunately.
            if (!result)
            {
                Assert.Fail("Expected: " +
                    ",".InsertBetween(expected.Select(x => Convert.ToString(x))) + "; was: " +
                    ",".InsertBetween(copy.Select(x => Convert.ToString(x))));
            }

            Engine.Log.Info("PASS: " + "Expected: " +
                    ",".InsertBetween(expected.Select(x => Convert.ToString(x))) + "; " + Environment.NewLine + "was: " +
                    ",".InsertBetween(actual.Select(x => Convert.ToString(x))));
        }

        public static string InsertBetween(this string delimiter, IEnumerable<string> items)
        {
            var builder = new StringBuilder();
            foreach (var item in items)
            {
                if (builder.Length != 0)
                {
                    builder.Append(delimiter);
                }
                builder.Append(item);
            }
            return builder.ToString();
        }

        public static IEnumerable<string> GenerateSplits(this string str, params char[] separators)
        {
            foreach (var split in str.Split(separators))
                yield return split;
        }
    }
}