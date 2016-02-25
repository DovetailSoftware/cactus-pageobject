using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Cactus.Drivers
{
    public static class DataTableExtensions
    {
        public static DataTable XmlToDataTable(this string xml)
        {
            var reader = new StringReader(xml);
            var ds = new DataSet();
            ds.ReadXml(reader);
            return ds.Tables[0];
        }

        public static string Join(this IEnumerable<string> values, string separator)
        {
            return Join(values.ToArray(), separator);
        }
    }
}
