using System;

namespace YourTech.IO {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class YronObjectAttribute : Attribute {
        public Type YroType {
            get {
                if (_yroType != null) return _yroType;
                if (!string.IsNullOrWhiteSpace(YroTypeName)) {
                    try { _yroType = Type.GetType(YroTypeName); } catch { }
                    YroTypeName = null;
                }
                return _yroType;
            }
            set { _yroType = value; }
        }
        private Type _yroType;

        public string YroTypeName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class YronPropertyAttribute : Attribute {
        public string Name { get; set; }
        public bool GetOnly { get; set; }

        public Type DefaultType {
            get {
                if (_defaultType != null) return _defaultType;
                if (!string.IsNullOrWhiteSpace(DefaultTypeName)) {
                    _defaultType = Type.GetType(DefaultTypeName);
                    DefaultTypeName = null;
                }
                return _defaultType;
            }
            set { _defaultType = value; }
        }
        private Type _defaultType;

        public string DefaultTypeName { get; set; }
    }
}
