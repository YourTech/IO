using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO;
using YourTech.IO.Yron;
using System.Reflection;

namespace YourTech.IO.Yron {
    public sealed class YronReader : StonReader<YronToken> {
        SortedDictionary<string, IYronType> _valueType;

        private object _rootObject;
        private IYronType _rootYronType;

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

        public YronReader(object obj, IYronType yronType = null) : base() {
            _valueType = new SortedDictionary<string, IYronType>();
            _rootObject = obj;
            _rootYronType = yronType;
        }

        protected override void Initialize() {
            _blockToken = new YronToken();
        }
        protected override bool OnReadTokens(YronToken blockToken) {
            if (!CanRead) return false;

            object value = null;
            Type valueType = null;
            IYronType yronType = null;
            string propertyName = null;

            if (blockToken.TokenType == StonTokenTypes.None) {
                value = _rootObject;
                yronType = _rootYronType;
            } else if ((blockToken.TokenType & StonTokenTypes.BeginBlock) != StonTokenTypes.None) {
                if (++blockToken.ItemIndex >= blockToken.ItemCount) {
                    _tokenQueue.Enqueue(new YronToken(StonTokenTypes.EndBlock));
                    return true;
                }
                value = blockToken.GetItem(blockToken.ItemIndex, out propertyName);
            }
            valueType = value?.GetType();
            string typeName = valueType?.AsString();

            if (yronType == null && valueType != null) {
                YronObjectAttribute att = valueType.GetCustomAttribute<YronObjectAttribute>();

                if (att == null) {
                    if (Type.GetTypeCode(valueType) == TypeCode.Object) yronType = YronListType.Instance;
                } else if (att.YroType != null) yronType = (IYronType)Activator.CreateInstance(att.YroType);
                else yronType = new YronObjectType(valueType);

                if (typeName != null) this[typeName] = yronType;
            }
            if (yronType == null) {
                _tokenQueue.Enqueue(new YronToken(value, propertyName));
            } else _tokenQueue.Enqueue(new YronToken(value, yronType, propertyName));
            return true;
        }
    }

    public class YronToken : StonToken {
        public IYronType YronType { get; set; }

        public object Object { get; private set; }
        public IList List { get; private set; }
        public Type ItemType { get; private set; }

        public int ItemIndex { get; set; }
        public int ItemCount {
            get { return YronType?.GetTokenCount(Object) ?? 0; }
        }

        public object GetItem(int index, out string propertyName) {
            if (YronType == null) {
                propertyName = null;
                return null;
            } else return YronType.GetToken(Object, index, out propertyName);
        }

        public YronToken() : this(StonTokenTypes.None) { }
        public YronToken(StonTokenTypes stontype) : base(stontype) {
            ItemIndex = -1;
        }
        public YronToken(object value, string propertyName) : this(StonTokenTypes.Value) {
            _value = value;
            _propertyName = propertyName;
        }
        public YronToken(object obj, IYronType yronType, string propertyName) : this(StonTokenTypes.BeginObject) {
            YronType = yronType;
            Object = obj;
            _propertyName = propertyName;
            if (yronType != null && (_tokenType = yronType.TokenType) == StonTokenTypes.BeginArray) {
                if ((List = Object as IList) != null) ItemType = List.GetType().GetListGenericArgument();
            }
        }
    }
}
