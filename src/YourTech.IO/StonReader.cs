using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YourTech.IO {
    public abstract class StonReader<T> : IStonReader where T : StonToken {
        protected Queue<T> _tokenQueue;
        protected Stack<T> _tokenStack;

        public int Level { get { return _tokenStack.Count(); } }
        public int LevelIndex { get { return _blockToken?.Count ?? -1; } }

        public StonToken BlockToken { get { return _blockToken; } }
        protected T _blockToken;

        public StonToken Token { get { return _token; } }
        private T _token;

        public bool CanRead { get; private set; }

        protected StonReader(bool ignoreInitialize = false) {
            _tokenQueue = new Queue<T>();
            _tokenStack = new Stack<T>();
            CanRead = true;
            if (!ignoreInitialize) Initialize();
        }

        protected virtual void Initialize() {
            _blockToken = default(T);
        }

        public virtual bool Read() {
            if (!CanRead) return false;

            StonTokenTypes type = _token?.TokenType ?? StonTokenTypes.None;
            if (type == StonTokenTypes.BeginObject || type == StonTokenTypes.BeginArray) {
                _tokenStack.Push(_blockToken = _token);
            } else if ((type == StonTokenTypes.EndBlock) && _tokenStack.Count > 0) {
                _tokenStack.Pop();
                if (_tokenStack.Count > 0) {
                    _blockToken = _tokenStack.Peek();
                } else {
                    if ((_blockToken = default(T)) != null) _blockToken.Count = -1;
                    return CanRead = false;
                }
            }

            _token = ReadToken(GetNextTokenType(type, _blockToken.TokenType), _blockToken);
            ++_blockToken.Count;
            return CanRead = (type != StonTokenTypes.EOF);
        }
        protected abstract bool OnReadTokens(T blockToken);
        protected virtual StonTokenTypes GetNextTokenType(StonTokenTypes tokenType, StonTokenTypes blockType) {
            if (tokenType == StonTokenTypes.None) return StonTokenTypes.BeginObject | StonTokenTypes.BeginArray | StonTokenTypes.EOF;
            if (tokenType != StonTokenTypes.EndBlock || blockType != StonTokenTypes.None)
                return StonTokenTypes.EndBlock | StonTokenTypes.Name;
            else return StonTokenTypes.EOF;
        }
        protected virtual T ReadToken(StonTokenTypes expectedTokenType, T blockToken) {
            if (_tokenQueue.Count == 0 && (!CanRead || !OnReadTokens(blockToken))) return default(T);

            T retVal = _tokenQueue.Dequeue();
            StonTokenTypes type = retVal.TokenType;
            if ((type & expectedTokenType) != type) {
                throw new StonException(CorrectMessage(retVal, $"Expected {expectedTokenType.ToString()} token but received {type.ToString()}"));
            } else if (blockToken.TokenType == StonTokenTypes.BeginObject
                  && (type & StonTokenTypes.Name) == type
                  && retVal.PropertyName == null) {
                throw new StonException(CorrectMessage(retVal, "Property name expected"));
            }
            return retVal;
        }
        protected virtual string CorrectMessage(T token, string message) {
            return message;
        }
        public virtual void Dispose() { }

        public virtual string GetHashValue(short hashKey) { throw new NotImplementedException(); }
    }
    public class StonToken {
        public int Count { get; internal set; }
        public StonTokenTypes TokenType { get; set; }
        public object Value { get; set; }
        public string PropertyName { get; set; }

        public StonToken(StonTokenTypes tokenType = StonTokenTypes.None, object value = null) {
            Count = 0;
            TokenType = tokenType;
            Value = value;
        }
    }
    [Flags]
    public enum StonTokenTypes {
        None = 0,
        EOF = (1 << 0),
        BeginObject = (1 << 1),
        BeginArray = (1 << 3),
        EndBlock = (1 << 2),
        Value = (1 << 4),

        Name = BeginObject | BeginArray | Value,
    }
}
