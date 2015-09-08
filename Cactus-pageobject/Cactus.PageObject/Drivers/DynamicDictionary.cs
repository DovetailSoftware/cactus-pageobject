using System.Collections.Generic;

namespace Cactus.Drivers
{

    public class DynamicDictionary : System.Dynamic.DynamicObject
    {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();

        public int Count
        {
            get { return dictionary.Count; }
        }

        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();
            //return dictionary.TryGetValue(name, out result);
            if (!dictionary.TryGetValue(name, out result))
                result = null;
            return true;
        }

        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
        {
            dictionary[binder.Name.ToLower()] = value;
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return dictionary.Keys;
        }
    }
}