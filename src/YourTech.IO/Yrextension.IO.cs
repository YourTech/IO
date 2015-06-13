using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO;

namespace YourTech {
    static partial class Yrextension {
        public static Type AsType(this object value) {
            if (value == null) return null;
            if (value is Type) return (Type)value;
            string name = value.ToType<string>();
            if (string.IsNullOrWhiteSpace(name)) return null;
            try { return Type.GetType(name); } catch { return null; }
        }
        public static string AsString(this object value) {
            if (value == null) return null;
            if (value is Type) {
                Type type = (Type)value;
                return $"{type.FullName}, {type.Assembly.GetName().Name}";
            } else return value.ToType<string>();
        }
        public static object ConvertTo(this object value, Type type, object defaultValue = null, bool allowConvertToString = false) {
            if (value != null && !(value is DBNull) && type != null) {
                Type valueType = value.GetType();
                if (valueType == type || type.IsAssignableFrom(valueType)) return value;
                if (typeof(IConvertible).IsAssignableFrom(valueType)) return Convert.ChangeType(value, type);
                TypeConverter tc = TypeDescriptor.GetConverter(type);
                if (tc != null) {
                    if (tc.CanConvertFrom(value.GetType())) {
                        return tc.ConvertFrom(value);
                    } else if (allowConvertToString && tc.CanConvertFrom(typeof(string))) {
                        TypeConverter vc = TypeDescriptor.GetConverter(value.GetType());
                        if (vc.CanConvertTo(typeof(string)))
                            return tc.ConvertFromString(vc.ConvertToString(value));
                    }
                }
            }
            return (type.IsValueType && defaultValue == null) ? Activator.CreateInstance(type) : defaultValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToType<T>(this object value, T defaultValue = default(T), bool allowConvertToString = false) {
            return (T)ConvertTo(value, typeof(T), defaultValue, allowConvertToString);
        }

    }
}
