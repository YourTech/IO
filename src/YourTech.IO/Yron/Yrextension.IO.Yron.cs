using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YourTech.IO;

namespace YourTech {
    static partial class Yrextension {
        public static void ScanYronProperties(this Type type, Action<YronPropertyAttribute, PropertyInfo> action) {
            if (type == null || action == null) return;
            PropertyInfo[] pis = type.GetProperties();
            for (int i = 0; i < pis.Length; i++) {
                PropertyInfo pi = pis[i];
                YronPropertyAttribute att = pi.GetCustomAttribute<YronPropertyAttribute>();
                if (att == null) continue;
                action(att, pi);
            }
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetListGenericArgument(this Type type) {
            return type.GetInterfaces().FirstOrDefault<Type>((t) => {
                return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IList<>));
            }).GetGenericArguments()[0];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Tuple<Type, Type> GetDictionaryGenericArgument(this Type type) {
            Type[] ts = type.GetInterfaces().FirstOrDefault<Type>((t) => {
                return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            }).GetGenericArguments();
            return new Tuple<Type, Type>(ts[0], ts[1]);
        }
    }
}
