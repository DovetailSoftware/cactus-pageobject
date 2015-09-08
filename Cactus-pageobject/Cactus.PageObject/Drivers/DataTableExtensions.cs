using System.Data;
using System.IO;

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

    }
}
