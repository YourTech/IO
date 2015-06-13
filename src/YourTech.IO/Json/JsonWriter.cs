using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace YourTech.IO.Json {
    public sealed class JsonWriter : StonWriter<StonNode> {
        private TextWriter _writer;
        private bool _autoDisposeWriter;

        private bool _isIndented;
        private bool _unicodeSupport;

        public JsonWriter(TextWriter writer, bool autoDisposeWriter = false, bool isIndented = false, bool unicodeSupport = false) {
            _writer = writer;
            _autoDisposeWriter = autoDisposeWriter;
            _isIndented = isIndented;
            _unicodeSupport = unicodeSupport;
        }

        private JsonWriter CheckNewLine(int level = 0) {
            if (!_isNewLine) return this;
            level += _nodeStack?.Count ?? 0;
            for (int i = 0; i < level; i++) {
                _writer.Write("\t");
            }
            _isNewLine = false;
            return this;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonWriter EndLine() {
            if (!_isIndented) return this;
            _writer.Write(Environment.NewLine);
            _isNewLine = true;
            return this;
        }
        private bool _isNewLine;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonWriter WriteText(string text) {
            _writer.Write(text);
            return this;
        }
        private JsonWriter WriteString(string text) {
            if (text == null) {
                _writer.Write("null");
                return this;
            }
            _writer.Write("\"");
            int len = text.Length;
            for (int i = 0; i < len; i++) {
                char chr = text[i];
                switch (chr) {
                    case '"': _writer.Write("\\\""); break;
                    //case '\'': _writer.Write("\\'"); break;
                    //case '/': _writer.Write("\\/"); break;
                    case '\\': _writer.Write("\\\\"); break;
                    case '\b': _writer.Write("\\b"); break;
                    case '\f': _writer.Write("\\f"); break;
                    case '\n': _writer.Write("\\n"); break;
                    case '\r': _writer.Write("\\r"); break;
                    case '\t': _writer.Write("\\t"); break;
                    default:
                        int value = (int)chr;
                        if ((value >= 32 && value < 127) || chr == '_') _writer.Write(chr);
                        else if (_unicodeSupport && ((value >= 0x80 && value <= 0xFF)
                            || (value >= 0x0080 && value <= 0x00FF)
                            || (value >= 0x0100 && value <= 0x024F)
                            || (value >= 0x0300 && value <= 0x036F)
                            || (value >= 0x1E00 && value <= 0x1EFF)))
                            _writer.Write(chr);
                        else {
                            _writer.Write("\\u");
                            _writer.Write(((int)chr).ToString("X").PadLeft(4, '0'));
                        }
                        break;
                }
            }
            _writer.Write("\"");
            return this;
        }
        private JsonWriter WriteValue(object value) {
            switch (Type.GetTypeCode(value?.GetType())) {
                case TypeCode.DBNull:
                case TypeCode.Empty: _writer.Write("null"); break;

                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal: _writer.Write(value.ToString()); break;

                case TypeCode.Boolean: _writer.Write((bool)value ? "true" : "false"); break;

                case TypeCode.DateTime:
                case TypeCode.Char: WriteString(value.ToString()); break;

                case TypeCode.String: WriteString((string)value); break;

                case TypeCode.Object: WriteString(value.ToType<string>()); break;
            }
            return this;
        }
        private void WritePropertyName(StonNode node, string propertyName) {
            if (node.Count > 1) WriteText(",").EndLine();
            if (node.TokenType == StonTokenTypes.BeginObject) {
                string name = propertyName;
                if (string.IsNullOrWhiteSpace(name)) throw new StonException("Expected Property Name");
                else CheckNewLine().WriteString(name).WriteText(_isIndented ? ": " : ":");
            }
            CheckNewLine();
        }

        protected override void Initialize() {
            _node = new StonNode();
        }

        public override void Write(object value, string propertyName = null) {
            WritePropertyName(Node, propertyName);
            WriteValue(value.AsString());
        }
        protected override StonNode OnBeginObject(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            WriteText("{").EndLine();
            StonNode retVal = new StonNode(StonTokenTypes.BeginObject);
            if (token.Value != null) {
                CheckNewLine(1).WriteString("$")
                    .WriteText(_isIndented ? ": " : ":")
                    .WriteValue(token.Value.AsString());
                retVal.Count++;
            }
            return retVal;
        }
        protected override StonNode OnBeginArray(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            WriteText("[").EndLine();
            StonNode retVal = new StonNode(StonTokenTypes.BeginArray);
            if (token.Value != null) {
                CheckNewLine(1).WriteString("$")
                    .WriteText(_isIndented ? ": " : ":")
                    .WriteValue(token.Value.AsString());
                retVal.Count++;
            }
            return retVal;
        }
        protected override void OnValue(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            WriteValue(token.Value.AsString());
        }
        protected override void OnEndBlock(StonNode node) {
            if (node.Count > 0) EndLine();
            if (node.TokenType == StonTokenTypes.BeginObject) {
                CheckNewLine(-1).WriteText("}");
            } else if (node.TokenType == StonTokenTypes.BeginArray) {
                CheckNewLine(-1).WriteText("]");
            }
        }

        public override void Dispose() {
            if (_writer != null) {
                try {
                    _writer.Flush();
                    if (_autoDisposeWriter) _writer.Dispose();
                } catch { }
                _writer = null;
            }
        }

    }
}
