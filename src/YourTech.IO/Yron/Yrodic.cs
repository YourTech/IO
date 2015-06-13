using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YourTech.IO.Yron {
    public class YrodicType : IYronType {
        Type _itemType;
        public YrodicType(Type itemType) {
            _itemType = itemType;
        }
        public IYronPropertyInfo GetProperty(string propertyName) {
            return new YrodicPropertyInfo(propertyName, _itemType);
        }
    }

    internal sealed class YrodicPropertyInfo : IYronPropertyInfo {
        public string PropertyName { get; private set; }
        public Type PropertyType { get; private set; }
        public Type DefaultType { get; private set; }
        public bool GetOnly { get { return false; } }

        public YrodicPropertyInfo(string propertyName, Type propertyType) {
            PropertyName = propertyName;
            DefaultType = PropertyType = propertyType;
        }

        public object GetValue(object obj) {
            IDictionary dic = obj as IDictionary;
            return dic == null || !dic.Contains(PropertyName) ? null : dic[PropertyName];
        }
        public void SetValue(object obj, object value) {
            IDictionary dic = obj as IDictionary;
            if (dic != null) dic[PropertyName] = value;
        }
    }
}
