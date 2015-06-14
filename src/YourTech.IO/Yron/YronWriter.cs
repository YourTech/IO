using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;

namespace YourTech.IO.Yron {
    public sealed class YronWriter : StonWriter<YronNode> {
        SortedDictionary<string, IYronType> _valueType;

        public object ReturnValue { get; private set; }
        private Type _objType;
        private IYronType _yronType;

        private IYronType this[string typeName] {
            get {
                if (typeName == null) return null;
                IYronType retVal;
                return _valueType.TryGetValue(typeName, out retVal) ? retVal : null;
            }
            set {
                if (typeName != null) _valueType[typeName] = value;
            }
        }

        public YronWriter() {
            _valueType = new SortedDictionary<string, IYronType>();
        }
        public YronWriter(object obj, Type objType = null) : this() {
            ReturnValue = obj;
            _objType = objType ?? obj?.GetType();
        }
        public YronWriter(object obj, IYronType yronType) : this() {
            ReturnValue = obj;
            _yronType = yronType;
        }

        protected override void Initialize() { _node = new YronNode(); }

        public override void Dispose() {
            if (_valueType != null) { _valueType.Clear(); _valueType = null; }
            base.Dispose();
        }

        protected override YronNode OnBeginObject(YronNode node, StonToken token) {
            IYronPropertyInfo pInfo = node.TokenType == StonTokenTypes.BeginObject ? node.YronType.GetProperty(token.PropertyName) : null;
            Type objType = token.Value.AsType()
                ?? (node.TokenType == StonTokenTypes.None ? ReturnValue?.GetType() ?? _objType
                : node.TokenType == StonTokenTypes.BeginObject ? pInfo?.PropertyType
                : node.TokenType == StonTokenTypes.BeginArray ? node.ItemType
                : null);

            object value = (node.Object != null && pInfo != null && pInfo.GetOnly ? pInfo.GetValue(node.Object) : null)
                ?? (node.TokenType == StonTokenTypes.None ? ReturnValue : null)
                ?? (objType != null ? Activator.CreateInstance(objType) : null);

            string typeName = objType.AsString();
            IYronType yronType = (node.TokenType == StonTokenTypes.None ? _yronType : null)
                ?? (objType == null ? null : this[typeName]);

            if (yronType == null && objType != null) {
                YronObjectAttribute att = objType.GetCustomAttribute<YronObjectAttribute>();

                if (att == null) {
                    if (Type.GetTypeCode(objType) == TypeCode.Object) yronType = YronListType.Instance;
                } else if (att.YroType != null) yronType = (IYronType)Activator.CreateInstance(att.YroType);
                else yronType = new YronObjectType(objType);

                if (typeName != null) this[typeName] = yronType;
            }

            YronNode retVal = new YronNode(value, yronType);
            node.SetValue(value, token.PropertyName);

            return retVal;
        }
        protected override YronNode OnBeginArray(YronNode node, StonToken token) {
            return OnBeginObject(node, token);
        }
        protected override void OnValue(YronNode node, StonToken token) {
            node.SetValue(token.Value, token.PropertyName);
        }
        protected override void OnEndBlock(YronNode node) { }
    }

    public interface IYronType {
        StonTokenTypes TokenType { get; }

        int GetTokenCount(object This);
        object GetToken(object This, int index, out string propertyName);

        IYronPropertyInfo GetProperty(string propertyName);
        void SetProperty(object This, string propertyName, object value);
        void AddItem(IList list, object value);
    }
    public interface IYronPropertyInfo {
        string PropertyName { get; }
        Type PropertyType { get; }
        Type DefaultType { get; }
        bool GetOnly { get; }

        object GetValue(object obj);
        void SetValue(object obj, object value);
    }
    public class YronNode : StonNode {
        public IYronType YronType { get; set; }

        public object Object { get; private set; }
        public IList List { get; private set; }
        public Type ItemType { get; private set; }

        public YronNode() : base(StonTokenTypes.None) { }
        public YronNode(object obj, IYronType yronType) : base(StonTokenTypes.BeginObject) {
            YronType = yronType;
            Object = obj;
            if (yronType != null && (_tokenType = yronType.TokenType) == StonTokenTypes.BeginArray) {
                if ((List = Object as IList) != null) ItemType = List.GetType().GetListGenericArgument();
            }
        }
        public void SetValue(object value, string propertyName) {
            if (Object == null) return;
            else if (TokenType == StonTokenTypes.BeginObject) {
                YronType?.SetProperty(Object, propertyName, value);
            } else if (TokenType == StonTokenTypes.BeginArray) {
                YronType?.AddItem(List, value);
            } else throw new StonException($"Invalid Node Type {TokenType}");
        }
    }

    internal class YronObjectType : IYronType {
        List<Tuple<string, YronPropertyInfo>> _propertyDic;

        public StonTokenTypes TokenType { get { return StonTokenTypes.BeginObject; } }

        public Type ObjectType { get; private set; }

        public YronObjectType(Type objectType) {
            _propertyDic = new List<Tuple<string, YronPropertyInfo>>();
            if ((ObjectType = objectType) != null) {
                ObjectType.ScanYronProperties((a, p) => {
                    _propertyDic.Add(new Tuple<string, YronPropertyInfo>(a.Name ?? p.Name, new YronPropertyInfo(a, p)));
                });
            }
            _propertyDic.Sort<Tuple<string, YronPropertyInfo>>((a, b) => { return string.Compare(a.Item1, b.Item1); });
        }


        public int GetTokenCount(object This) {
            return _propertyDic.Count;
        }
        public object GetToken(object This, int index, out string propertyName) {
            if (index < 0 || index >= _propertyDic.Count) {
                propertyName = null;
                return null;
            }
            Tuple<string, YronPropertyInfo> pInfo = _propertyDic[index];
            propertyName = pInfo.Item1;
            return pInfo.Item2.GetValue(This);
        }

        public IYronPropertyInfo GetProperty(string propertyName) {
            if (string.IsNullOrEmpty(propertyName)) return null;
            int index = _propertyDic.BinarySearch((a) => { return string.Compare(a.Item1, propertyName); });
            return index < 0 ? null : _propertyDic[index].Item2;
        }
        public void SetProperty(object This, string propertyName, object value) {
            if (This == null) return;
            if (GetProperty(propertyName) == null) {
                Debug.WriteLine(propertyName);
            }
            GetProperty(propertyName)?.SetValue(This, value);
        }
        public void AddItem(IList list, object value) {
            throw new NotImplementedException();
        }
    }
    internal class YronListType : IYronType {
        static YronObjectAttribute _att = new YronObjectAttribute();
        public static YronListType Instance { get { return _instance; } }
        public static YronListType _instance = new YronListType();

        public StonTokenTypes TokenType { get { return StonTokenTypes.BeginArray; } }
        public YronObjectAttribute YronAttribute { get { return _att; } }

        private YronListType() { }

        public IYronPropertyInfo GetProperty(string propertyName) {
            throw new NotImplementedException();
        }

        public object GetToken(object This, int index, out string propertyName) {
            propertyName = null;
            if (This == null) return null;
            IList list = (IList)This;
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }
        public int GetTokenCount(object This) {
            return (This as IList)?.Count ?? 0;
        }

        public void SetProperty(object This, string propertyName, object value) {
            throw new NotImplementedException();
        }

        public void AddItem(IList list, object value) {
            list?.Add(value);
        }
    }
    internal class YronPropertyInfo : IYronPropertyInfo {
        private PropertyInfo _pInfo;

        public bool GetOnly { get; private set; }
        public string PropertyName { get; private set; }
        public Type PropertyType { get; private set; }
        public Type DefaultType { get; private set; }

        public YronPropertyInfo(YronPropertyAttribute att, PropertyInfo pInfo) {
            GetOnly = att?.GetOnly ?? false;
            PropertyName = att?.Name ?? pInfo?.Name;
            PropertyType = pInfo?.PropertyType;
            DefaultType = att?.DefaultType ?? PropertyType;
            _pInfo = pInfo;
        }

        public object GetValue(object obj) {
            return _pInfo?.GetValue(obj);
        }
        public void SetValue(object obj, object value) {
            _pInfo?.SetValue(obj, value);
        }
    }
}
