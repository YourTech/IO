using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace YourTech.IO.Bon {
    public sealed class BonReader : StonReader<StonToken> {
        SortedDictionary<short, string> _hashTable;

        Stream _reader;
        bool _autoDisposeReader;

        public BonReader(Stream reader, bool autoDisposeReader = false) : base() {
            _hashTable = new SortedDictionary<short, string>();
            _reader = reader;
            _autoDisposeReader = autoDisposeReader;
        }

        protected override void Initialize() {
            _blockToken = new StonToken();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ReadByte(out byte value) {
            int data = _reader.ReadByte();
            if (data < 0) {
                value = 0; return false;
            } else {
                value = (byte)data; return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ReadString(bool isSmall = true) {
            return System.Text.Encoding.UTF8.GetString(_reader.ReadBuffer(isSmall ? _reader.ReadByte() : _reader.ReadInt32()));
        }

        private string Caches(short key, string text) {
            string old;
            if (_hashTable.TryGetValue(key, out old)) {
                if (old != text) throw new StonException($"Conflit Key: Old = {old}, New = {text}");
            } else _hashTable[key] = text;
            return text;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetText(short key) {
            string text;
            return _hashTable.TryGetValue(key, out text) ? text : null;
        }

        private StonToken ReadToken(StonToken blockToken) {
            byte chr = 0;
            while (true) {
                if (!ReadByte(out chr)) return new StonToken(StonTokenTypes.EOF);

                if (chr == BonMarker.ProperName) return new StonToken(StonTokenTypes.Name, null) { PropertyName = GetText(_reader.ReadInt16()) };
                else if (chr == BonMarker.PropertHash) return new StonToken(StonTokenTypes.Name, null) { PropertyName = Caches(_reader.ReadInt16(), ReadString()) };
                else if (chr == BonMarker.ObjectType) return new StonToken(StonTokenTypes.Name, _reader.ReadInt16()) { PropertyName = "$" };
                else if (chr == BonMarker.ObjectHash) return new StonToken(StonTokenTypes.Name, new System.Tuple<short, string>(_reader.ReadInt16(), ReadString())) { PropertyName = "$" };
                else if (chr == BonMarker.BeginArray) return new StonToken(StonTokenTypes.BeginArray);
                else if (chr == BonMarker.EndArray) {
                    if (blockToken.TokenType == StonTokenTypes.BeginObject) throw new StonException("Expected EndObject Token");
                    return new StonToken(StonTokenTypes.EndBlock);
                } else if (chr == BonMarker.BeginObject) return new StonToken(StonTokenTypes.BeginObject);
                else if (chr == BonMarker.EndObject) {
                    if (blockToken.TokenType == StonTokenTypes.BeginArray) throw new StonException("Expected EndArray Token");
                    return new StonToken(StonTokenTypes.EndBlock);
                } else if (chr == BonMarker.NewLine) continue;
                else if (chr == BonMarker.Null) return new StonToken(StonTokenTypes.Value);
                else if (chr == BonMarker.True) return new StonToken(StonTokenTypes.Value, true);
                else if (chr == BonMarker.False) return new StonToken(StonTokenTypes.Value, false);
                else if (chr == BonMarker.Int8) return new StonToken(StonTokenTypes.Value, (byte)_reader.ReadByte());
                else if (chr == BonMarker.UInt8) return new StonToken(StonTokenTypes.Value, (sbyte)_reader.ReadByte());
                else if (chr == BonMarker.Int16) return new StonToken(StonTokenTypes.Value, _reader.ReadInt16());
                else if (chr == BonMarker.Int32) return new StonToken(StonTokenTypes.Value, _reader.ReadInt32());
                else if (chr == BonMarker.Int64) return new StonToken(StonTokenTypes.Value, _reader.ReadInt64());
                else if (chr == BonMarker.Float32) return new StonToken(StonTokenTypes.Value, _reader.ReadSingle());
                else if (chr == BonMarker.Float64) return new StonToken(StonTokenTypes.Value, _reader.ReadDouble());
                else if (chr == BonMarker.Char) return new StonToken(StonTokenTypes.Value, (char)_reader.ReadInt16());
                else if (chr == BonMarker.Decimal) return new StonToken(StonTokenTypes.Value, decimal.Parse(ReadString()));
                else if (chr == BonMarker.String) return new StonToken(StonTokenTypes.Value, ReadString());
                else if (chr == BonMarker.Text) return new StonToken(StonTokenTypes.Value, ReadString(false));
                else if (chr == BonMarker.CarierReturn) continue;
                else if (chr == BonMarker.Tab) continue;
                else if (chr == BonMarker.WhiteSpace) continue;
                else throw new StonException($"Not support Marker {(char)chr}");
            }
        }

        protected override bool OnReadTokens(StonToken blockToken) {
            StonToken token = ReadToken(blockToken);
            if (token.TokenType == StonTokenTypes.Name) {
                string propertyName = token.PropertyName;
                token = ReadToken(blockToken);
                token.PropertyName = propertyName;
            }
            bool isObject;
            if ((isObject = (token.TokenType == StonTokenTypes.BeginObject)) || token.TokenType == StonTokenTypes.BeginArray) {
                StonToken objToken = token;

                if ((token = ReadToken(blockToken)).TokenType != StonTokenTypes.Name && isObject)
                    throw new StonException("Expected Property Name Token");

                if (token.PropertyName.ToType<string>() == "$") {
                    objToken.Value = token.Value;
                    _tokenQueue.Enqueue(objToken);
                } else {
                    _tokenQueue.Enqueue(objToken);
                    if (token.TokenType == StonTokenTypes.Name) {
                        string propertyName = token.PropertyName;
                        token = ReadToken(blockToken);
                        token.PropertyName = propertyName;
                    }
                    _tokenQueue.Enqueue(token);
                }
            } else _tokenQueue.Enqueue(token);
            return true;
        }
        public override void Dispose() {
            if (_reader != null) {
                if (_autoDisposeReader) try { _reader.Dispose(); } catch { }
                _reader = null;
            }
        }
    }
}
