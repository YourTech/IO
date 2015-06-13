using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace YourTech.IO.Json {
    public sealed class JsonReader : StonReader<StonToken> {
        const char EOFCharacter = '\0';

        TextReader _reader;
        bool _autoDisposeReader;
        int _row, _column;

        public JsonReader(TextReader reader, bool autoDisposeReader = false) : base() {
            _reader = reader;
            _autoDisposeReader = autoDisposeReader;
            _row = 0;
            _column = 0;
        }

        protected override void Initialize() {
            _blockToken = NewToken();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char ToChar(int value) { return value <= 0 ? EOFCharacter : (char)value; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char PeekChar() {
            if (_escapseCharater != EOFCharacter) return _escapseCharater;
            char retVal = ToChar(_reader.Peek());
            if (retVal == '\\') retVal = _escapseCharater = ReadJsonEscapseCharacter(retVal);
            return retVal;
        }
        char _escapseCharater = EOFCharacter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonReader MoveNext() {
            char chr = _escapseCharater;
            if (_escapseCharater != EOFCharacter) {
                _escapseCharater = EOFCharacter;
            } else {
                chr = ToChar(_reader.Read());
                if (chr == '\n') {
                    _row++;
                    _column = 0;
                } else if (chr != '\r') _column++;
            }
            return this;
        }
        private char ReadJsonEscapseCharacter(char chr) {
            if (chr != '\\') return chr;
            _reader.Read();
            StringBuilder u = new StringBuilder();
            switch (chr = ToChar(_reader.Peek())) {
                case '"': MoveNext(); return '\"';
                case '\'': MoveNext(); return '\'';
                case '\\': MoveNext(); return '\\';
                case '/': MoveNext(); return '/';
                case 'b': MoveNext(); return '\b';
                case 'f': MoveNext(); return '\f';
                case 'n': MoveNext(); return '\n';
                case 'r': MoveNext(); return '\r';
                case 't': MoveNext(); return '\t';
                case 'u':
                case 'U':
                    if (char.IsDigit(chr = MoveNext().PeekChar()) || "abcdef".IndexOf(char.ToLower(chr)) > -1) u.Append(chr);
                    else throw new StonException(CorrectMessage(NewToken(), $"Invalid Escapse Unicode Character '{chr}'"));
                    if (char.IsDigit(chr = MoveNext().PeekChar()) || "abcdef".IndexOf(char.ToLower(chr)) > -1) u.Append(chr);
                    else throw new StonException(CorrectMessage(NewToken(), $"Invalid Escapse Unicode Character '{chr}'"));
                    if (char.IsDigit(chr = MoveNext().PeekChar()) || "abcdef".IndexOf(char.ToLower(chr)) > -1) u.Append(chr);
                    else throw new StonException(CorrectMessage(NewToken(), $"Invalid Escapse Unicode Character '{chr}'"));
                    if (char.IsDigit(chr = MoveNext().PeekChar()) || "abcdef".IndexOf(char.ToLower(chr)) > -1) u.Append(chr);
                    else throw new StonException(CorrectMessage(NewToken(), $"Invalid Escapse Unicode Character '{chr}'"));
                    MoveNext();
                    return (char)(short.Parse(u.ToString(), System.Globalization.NumberStyles.HexNumber));
                default: throw new StonException(CorrectMessage(NewToken(), $"Invalid Escapse character '{chr}'"));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonToken NewToken(StonTokenTypes type = StonTokenTypes.None, object value = null, string name = null) {
            if (_previousIsSeparator != null) {
                if (type == StonTokenTypes.EndBlock || type == StonTokenTypes.EOF)
                    throw new StonException(CorrectMessage(_previousIsSeparator, "Invalid Separator Token"));
                _previousIsSeparator = null;
            }
            return new JsonToken(_row, _column, type, value) { PropertyName = name };
        }

        private JsonToken ReadToken(StonToken blockToken) {
            char chr = EOFCharacter;

            if (char.IsWhiteSpace(chr = PeekChar())) {
                while ((chr = MoveNext().PeekChar()) != EOFCharacter) if (!char.IsWhiteSpace(chr)) break;
            }
            if (chr == EOFCharacter) return NewToken(StonTokenTypes.EOF);

            StringBuilder sb = new StringBuilder();
            if (chr == '"') {
                while ((chr = MoveNext().PeekChar()) != EOFCharacter) {
                    if (chr != _escapseCharater && chr == '"') {
                        while ((chr = MoveNext().PeekChar()) != EOFCharacter) if (!char.IsWhiteSpace(chr)) break;
                        if (chr == ':') return MoveNext().NewToken(StonTokenTypes.Name, null, sb.ToString());
                        return NewToken(StonTokenTypes.Value, sb.ToString());
                    } else sb.Append(chr);
                }
                throw new StonException(CorrectMessage(NewToken(), "Expected \" character"));
            } else if (chr == '{') return MoveNext().NewToken(StonTokenTypes.BeginObject, null, null);
            else if (chr == '[') return MoveNext().NewToken(StonTokenTypes.BeginArray, null, null);
            else if (chr == '}') {
                if (blockToken.TokenType == StonTokenTypes.BeginArray) throw new StonException(CorrectMessage(NewToken(), "Expected EndArray Token"));
                else return MoveNext().NewToken(StonTokenTypes.EndBlock, null, null);
            } else if (chr == ']') {
                if (blockToken.TokenType == StonTokenTypes.BeginObject) throw new StonException(CorrectMessage(NewToken(), "Expected EndObject Token"));
                else return MoveNext().NewToken(StonTokenTypes.EndBlock, null, null);
            } else if (chr == ',') {
                if (_previousIsSeparator != null) throw new StonException(CorrectMessage(NewToken(), "Unexpected Character ,"));
                _previousIsSeparator = NewToken();
                return MoveNext().ReadToken(blockToken);
            } else {
                sb.Append(chr);
                bool isFloat = false;
                char decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
                while ((chr = MoveNext().PeekChar()) != EOFCharacter) {
                    if (!isFloat && chr == decimalSeparator) isFloat = true;
                    if ("{}[],:\"".IndexOf(chr) > -1) break;
                    else if (char.IsWhiteSpace(chr)) {
                        while ((chr = MoveNext().PeekChar()) != EOFCharacter) if (!char.IsWhiteSpace(chr)) break;
                        break;
                    } else sb.Append(chr);
                }
                if (chr == ':') throw new StonException(CorrectMessage(NewToken(), "Unexpected character :"));
                else {
                    string value = sb.ToString();
                    if (value == "null") return NewToken(StonTokenTypes.Value, null);
                    else if (value == "true") return NewToken(StonTokenTypes.Value, true);
                    else if (value == "false") return NewToken(StonTokenTypes.Value, false);
                    else if (isFloat) return NewToken(StonTokenTypes.Value, double.Parse(value));
                    else return NewToken(StonTokenTypes.Value, int.Parse(value));
                }
            }
        }
        JsonToken _previousIsSeparator;

        protected override bool OnReadTokens(StonToken blockToken) {
            JsonToken token = ReadToken(blockToken);
            if (token.TokenType == StonTokenTypes.Name) {
                string propertyName = token.PropertyName;
                token = ReadToken(blockToken);
                token.PropertyName = propertyName;
            }
            bool isObject;
            if ((isObject = (token.TokenType == StonTokenTypes.BeginObject)) || token.TokenType == StonTokenTypes.BeginArray) {
                JsonToken objToken = token;

                if ((token = ReadToken(blockToken)).TokenType != StonTokenTypes.Name && isObject)
                    throw new StonException(CorrectMessage(NewToken(), "Expected Property Name Token"));

                if (string.Compare(token.PropertyName.ToType<string>(), "$", true) == 0) {
                    if ((token = ReadToken(blockToken)).TokenType != StonTokenTypes.Value) throw new StonException(CorrectMessage(NewToken(), "Expected Type Name"));
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
        protected override string CorrectMessage(StonToken token, string message) {
            JsonToken jt = (JsonToken)token;
            return $"At [{jt.Row + 1},{jt.Column + 1}]: {message}";
        }
        public override void Dispose() {
            if (_reader != null) {
                if (_autoDisposeReader) try { _reader.Dispose(); } catch { }
                _reader = null;
            }
        }
    }
    class JsonToken : StonToken {
        public int Row { get; private set; }
        public int Column { get; private set; }
        public JsonToken(int row, int column, StonTokenTypes tokenType = StonTokenTypes.None, object value = null) : base(tokenType, value) {
            Row = row;
            Column = column;
        }
    }
}
