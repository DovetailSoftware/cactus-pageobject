using System.Collections.Generic;
using Cactus.Drivers;

namespace Cactus.DataForTests
{
    class SearchData
    {
        public static dynamic GenerateSampleData()
        {
            dynamic dataObject = new List<dynamic>();
            var i = 0;

            dataObject.Add(new DynamicDictionary());
            dataObject[i].Name = "Color";
            dataObject[i].DataType = "Text";
            dataObject[i].Data = new List<string>() { "Ivory", "Beige", "Wheat", "Tan", "Khaki", "Silver", "Gray", "Charcoal", "Navy Blue", "Royal Blue", "Medium Blue", "Azure", "Cyan", "Aquamarine", "Teal", "Forest Green", "Olive", "Chartreuse", "Lime", "Golden", "Goldenrod", "Coral", "Orange", "Salmon", "Hot Pink", "Fuchsia", "Puce", "Mauve", "Lavender", "Plum", "Indigo", "Maroon", "Crimson" };


            i++;
            dataObject.Add(new DynamicDictionary());
            dataObject[i].Name = "Size";
            dataObject[i].DataType = "Text";
            dataObject[i].Data = new List<string>() { "x-small", "small", "medium", "large" };



            return dataObject;
        }
    }
}
