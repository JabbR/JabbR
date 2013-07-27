using System.Collections.Generic;
using System.Dynamic;

namespace JabbR.Services
{
    public class DynamicModel : DynamicObject
    {
        private readonly IDictionary<string, object> _propertyMap;

        public DynamicModel(IDictionary<string, object> propertyMap)
        {
            if (propertyMap == null)
            {
                throw new System.ArgumentNullException("propertyMap");
            }

            _propertyMap = propertyMap;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _propertyMap.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            return binder != null && _propertyMap.TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (binder != null)
            {
                _propertyMap[binder.Name] = value;

                return true;
            }

            return false;
        }
    }
}