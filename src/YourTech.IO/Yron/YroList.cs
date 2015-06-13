using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YourTech.IO.Yron {
    public class YrolistType : IYronType {
        Type _itemType;
        private Action<object, string> _headerSetter;
        private Func<object, string> _headerGetter;

        public YrolistType(Type itemType, string headerPropertyName) {
            _itemType = itemType;
            PropertyInfo pInfo = _itemType?.GetProperty(headerPropertyName);
            if (pInfo != null) {
                _headerSetter = (o, v) => { pInfo.SetValue(o, v); };
                _headerGetter = (o) => { return pInfo.GetValue(o) as string; };
            }
        }
        public IYronPropertyInfo GetProperty(string propertyName) {
            return new YrolistPropertyInfo(propertyName, _itemType, _headerSetter, _headerGetter);
        }
    }

    internal sealed class YrolistPropertyInfo : IYronPropertyInfo {
        private Action<object, string> _headerSetter;
        private Func<object, string> _headerGetter;

        public string PropertyName { get; private set; }
        public Type PropertyType { get; private set; }
        public Type DefaultType { get; private set; }
        public bool GetOnly { get { return false; } }

        public YrolistPropertyInfo(string propertyName, Type propertyType, Action<object, string> headerSetter, Func<object, string> headerGetter) {
            PropertyName = propertyName;
            DefaultType = PropertyType = propertyType;
            _headerSetter = headerSetter;
            _headerGetter = headerGetter;
        }

        public object GetValue(object obj) {
            throw new NotSupportedException();
        }
        public void SetValue(object obj, object value) {
            if (_headerSetter != null && value != null) _headerSetter(value, PropertyName);
            (obj as IList)?.Add(value);
        }
    }
}
