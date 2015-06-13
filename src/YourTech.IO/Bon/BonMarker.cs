using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourTech.IO.Bon {
    public static class BonMarker {
        public const byte Null = (byte)'Z';//1
        public const byte True = (byte)'T';//1
        public const byte False = (byte)'F';//1
        public const byte Int8 = (byte)'i';//2
        public const byte UInt8 = (byte)'u';//2
        public const byte Int16 = (byte)'I';//3
        public const byte Int32 = (byte)'l';//5
        public const byte Int64 = (byte)'L';//9
        public const byte Float32 = (byte)'d';//5
        public const byte Float64 = (byte)'D';//9
        public const byte Decimal = (byte)'H';
        public const byte Char = (byte)'c';//2
        public const byte String = (byte)'s';
        public const byte Text = (byte)'t';
        public const byte BeginArray = (byte)'[';//1
        public const byte EndArray = (byte)']';//1
        public const byte BeginObject = (byte)'{';//1
        public const byte EndObject = (byte)'}';//1

        public const byte ObjectHash = (byte)'#';//1
        public const byte ObjectType = (byte)'$';//1
        public const byte ProperName = (byte)'*';//1
        public const byte PropertHash = (byte)'@';//1

        public const byte NewLine = (byte)'\n';//1
        public const byte CarierReturn = (byte)'\r';//1
        public const byte Tab = (byte)'\t';//1
        public const byte WhiteSpace = (byte)' ';//1
    }
}
