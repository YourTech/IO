using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace YourTech.IO.Bon {
    public sealed class BonWriter : StonWriter<StonNode> {
        private SortedDictionary<string, short> _hashDic;
        private short _hashID;

        private Stream _writer;
        private bool _autoDisposeWriter;

        private bool _isIndented;
        private bool _unicodeSupport;

        public BonWriter(Stream writer, bool autoDisposeWriter = false, bool unicodeSupport = true, bool isIndented = false) {
            _hashDic = new SortedDictionary<string, short>();
            _hashID = 0;

            _writer = writer;
            _autoDisposeWriter = autoDisposeWriter;
            _isIndented = isIndented;
            _unicodeSupport = unicodeSupport;
        }

        private void CheckNewLine(int level = 0) {
            if (!_isNewLine) return;
            level += _nodeStack?.Count ?? 0;
            for (int i = 0; i < level; i++) {
                _writer.WriteByte((byte)'\t');
            }
            _isNewLine = false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndLine() {
            if (!_isIndented) return;
            _writer.WriteByte((byte)'\r');
            _writer.WriteByte((byte)'\n');
            _isNewLine = true;
        }
        private bool _isNewLine;

        private void WriteString(string text, byte marker = 0) {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            int length = data.Length;
            if (length <= byte.MaxValue) {
                _writer.WriteByte(marker == 0 ? BonMarker.String : marker);
                _writer.WriteByte((byte)length);
            } else {
                _writer.WriteByte(marker == 0 ? BonMarker.Text : marker);
                _writer.WriteInt32(length);
            }
            _writer.Write(data, 0, length);
        }
        private void WriteValue(object value) {
            switch (Type.GetTypeCode(value?.GetType())) {
                case TypeCode.DBNull:
                case TypeCode.Empty: _writer.WriteByte(BonMarker.Null); break;

                case TypeCode.SByte:
                    _writer.WriteByte(BonMarker.UInt8);
                    _writer.WriteByte((byte)value);
                    break;
                case TypeCode.Byte:
                    _writer.WriteByte(BonMarker.Int8);
                    _writer.WriteByte((byte)value);
                    break;
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    _writer.WriteByte(BonMarker.Int16);
                    _writer.WriteInt16((Int16)value);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    _writer.WriteByte(BonMarker.Int32);
                    _writer.WriteInt32((Int32)value);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    _writer.WriteByte(BonMarker.Int64);
                    _writer.WriteInt64((Int64)value);
                    break;
                case TypeCode.Single:
                    _writer.WriteByte(BonMarker.Float32);
                    _writer.WriteSingle((Single)value);
                    break;
                case TypeCode.Double:
                    _writer.WriteByte(BonMarker.Float64);
                    _writer.WriteDouble((Double)value);
                    break;

                case TypeCode.Boolean:
                    _writer.WriteByte((bool)value ? BonMarker.True : BonMarker.False);
                    break;

                case TypeCode.Char:
                    _writer.WriteByte(BonMarker.Char);
                    _writer.WriteInt16((Int16)value);
                    break;
                case TypeCode.Decimal:
                    WriteString(value.ToString(), BonMarker.Decimal);
                    break;

                case TypeCode.DateTime:
                    WriteString(value.ToString());
                    break;
                case TypeCode.String:
                    WriteString((string)value);
                    break;

                case TypeCode.Object: WriteString(value.ToType<string>()); break;
            }
        }
        private void WriteHash(string value, bool isType = false) {
            if (string.IsNullOrWhiteSpace(value)) { WriteValue(null); return; }
            short key;
            if (!_hashDic.TryGetValue(value, out key)) {
                _hashDic[value] = (key = ++_hashID);
                _writer.WriteByte(isType ? BonMarker.ObjectHash : BonMarker.PropertHash);
                _writer.WriteInt16(key);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
                _writer.WriteByte((byte)(data.Length));
                _writer.Write(data, 0, data.Length);
            } else {
                _writer.WriteByte(isType ? BonMarker.ObjectType : BonMarker.ProperName);
                _writer.WriteInt16(key);
            }
        }

        protected override void Initialize() {
            _node = new StonNode(StonTokenTypes.None);
        }

        private void WritePropertyName(StonNode node, string propertyName) {
            if (node.Count > 1) EndLine();
            if (node.TokenType == StonTokenTypes.BeginObject) {
                CheckNewLine();
                if (propertyName == null) throw new StonException($"Expected property name");
                else WriteHash(propertyName);
            }
            CheckNewLine();
        }
        protected override StonNode OnBeginObject(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            _writer.WriteByte(BonMarker.BeginObject); EndLine();
            StonNode retVal = new StonNode(StonTokenTypes.BeginObject);
            if (token.Value != null) {
                CheckNewLine(1);
                WriteHash(token.Value.AsString(), true);
                retVal.Count++;
            }
            return retVal;
        }
        protected override StonNode OnBeginArray(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            _writer.WriteByte(BonMarker.BeginArray); EndLine();
            StonNode retVal = new StonNode(StonTokenTypes.BeginArray);
            if (token.Value != null) {
                CheckNewLine(1);
                WriteHash(token.Value.AsString(), true);
                retVal.Count++;
            }
            return retVal;
        }
        protected override void OnValue(StonNode node, StonToken token) {
            WritePropertyName(node, token.PropertyName);
            WriteValue(token.Value);
        }
        protected override void OnEndBlock(StonNode node) {
            if (node.Count > 0) EndLine();
            if (node.TokenType == StonTokenTypes.BeginObject) {
                CheckNewLine(-1);
                _writer.WriteByte(BonMarker.EndObject);
            } else if (node.TokenType == StonTokenTypes.BeginArray) {
                CheckNewLine(-1);
                _writer.WriteByte(BonMarker.EndArray);
            }
        }

        public override void Dispose() {
            if (_writer != null) {
                try {
                    if (_autoDisposeWriter) _writer.Dispose();
                    else _writer.Flush();
                } catch { }
                _writer = null;
            }
        }

    }
}
