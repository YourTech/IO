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
        private Dictionary<object, string[]> _keyDic;
        public StonTokenTypes TokenType { get { return StonTokenTypes.BeginObject; } }

        public YrodicType(Type itemType) {
            _itemType = itemType;
            _keyDic = new Dictionary<object, string[]>();
        }

        public void AddItem(IList list, object value) {
            throw new NotImplementedException();
        }

        public IYronPropertyInfo GetProperty(string propertyName) {
            return new YrodicPropertyInfo(propertyName, _itemType);
        }
        public object GetToken(object This, int index, out string propertyName) {
            IDictionary dic = This as IDictionary;
            if (dic == null) { propertyName = null; return null; }

            string[] keys;
            if (!_keyDic.TryGetValue(This, out keys)) {
                keys = new string[dic.Count];
                dic.Keys.CopyTo(keys, 0);
                _keyDic[This] = keys;
            }

            if (index < 0 || index >= keys.Length) { propertyName = null; return null; }
            return dic.Contains(propertyName = keys[index]) ? dic[propertyName] : null;
        }
        public int GetTokenCount(object This) {
            return (This as ICollection)?.Count ?? 0;
        }
        public void SetProperty(object This, string propertyName, object value) {
            IDictionary dic = This as IDictionary;
            if (dic == null || string.IsNullOrWhiteSpace(propertyName)) return;
            dic[propertyName] = value;
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
