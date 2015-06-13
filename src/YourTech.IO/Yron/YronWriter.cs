using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;

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
            Type objType = token.Value.AsType()
                ?? (node.TokenType == StonTokenTypes.None ? ReturnValue?.GetType() ?? _objType
                : node.TokenType == StonTokenTypes.BeginObject ? node.YronType.GetProperty(token.PropertyName)?.PropertyType
                : node.TokenType == StonTokenTypes.BeginArray ? node.ItemType
                : null);


            object value = (node.TokenType == StonTokenTypes.None ? ReturnValue : null)
                ?? (objType != null ? Activator.CreateInstance(objType) : null);

            string typeName = null;
            IYronType yronType = (node.TokenType == StonTokenTypes.None ? _yronType : null)
                ?? (objType == null ? null : this[typeName = objType.AsString()]);
            if (yronType == null) {
                yronType = new YronType(objType);
                if (typeName != null) this[typeName] = yronType;
            }

            YronNode retVal = node.CreateNode(value, yronType);
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
        IYronPropertyInfo GetProperty(string propertyName);
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
            if ((List = (Object = obj) as IList) != null) ItemType = List.GetType().GetListGenericArgument();
            YronType = yronType;
        }

        //protected virtual IYronType GetYronType(YronWriter writer, object readerType, ref Type valueType) {
        //    valueType = readerType.AsType() ?? valueType;
        //    string text = valueType?.AsString();
        //    IYronType retVal = writer[text];
        //    if (retVal != null) return retVal;
        //    if (valueType != null) retVal = OnCreateYronType(valueType);
        //    if (!string.IsNullOrWhiteSpace(text)) writer[text] = retVal;
        //    return retVal;
        //}
        //protected virtual IYronType OnCreateYronType(Type type) {
        //    YronObjectAttribute att = type?.GetCustomAttribute<YronObjectAttribute>();
        //    if (att == null) throw new StonException($"Missing YronObjectAttribute on Type {type.AsString()}");
        //    return att.YronType != null ? (IYronType)Activator.CreateInstance(att.YronType) : new YronType(type, att);
        //}

        public Type GetValueType(StonToken token, ref object value) {
            IYronPropertyInfo pInfo = TokenType == StonTokenTypes.BeginObject ? YronType?.GetProperty(token.PropertyName) : null;
            if (pInfo != null && pInfo.GetOnly) value = pInfo.GetValue(Object);
            return value?.GetType() ?? pInfo?.DefaultType ?? ItemType;
        }
        public YronNode CreateNode(object value, IYronType YronType) {
            return new YronNode(value, YronType);
        }
        public void SetValue(object value, string propertyName) {
            if (Object == null) return;
            if (TokenType == StonTokenTypes.BeginObject) {
                IYronPropertyInfo pInfo = YronType?.GetProperty(propertyName);
                pInfo?.SetValue(Object, value.ConvertTo(pInfo.PropertyType));
            } else if (TokenType == StonTokenTypes.BeginArray) {
                if (List != null) List.Add(ItemType != null ? value.ConvertTo(ItemType) : value);
            } else throw new StonException($"Invalid Node Type {TokenType}");
        }
    }

    internal class YronType : IYronType {
        SortedDictionary<string, YronPropertyInfo> _propertyDic;
        public YronObjectAttribute YronAttribute { get; private set; }
        public Type ObjectType { get; private set; }

        public YronType(Type objectType, YronObjectAttribute att = null) {
            _propertyDic = new SortedDictionary<string, YronPropertyInfo>();
            YronAttribute = att ?? objectType?.GetCustomAttribute<YronObjectAttribute>();
            if ((ObjectType = objectType) != null) {
                ObjectType.ScanYronProperties((a, p) => {
                    _propertyDic[a.Name ?? p.Name] = new YronPropertyInfo(a, p);
                });
            }
        }

        public IYronPropertyInfo GetProperty(string propertyName) {
            if (string.IsNullOrEmpty(propertyName)) return null;
            YronPropertyInfo retVal;
            return _propertyDic.TryGetValue(propertyName, out retVal) ? retVal : null;
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
