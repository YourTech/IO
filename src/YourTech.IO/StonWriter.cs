using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace YourTech.IO {
    public abstract class StonWriter<N> : IStonWriter where N : StonNode {
        private short _hashID;
        protected Stack<N> _nodeStack;
        protected bool _forceEnd;

        public StonNode Node { get { return _node; } }
        protected N _node;

        protected StonWriter(bool ignoreInitialize = false) {
            _hashID = 0;
            _nodeStack = new Stack<N>();
            if (!ignoreInitialize) Initialize();
        }

        protected virtual void Initialize() { }

        public virtual void Dispose() { }

        public virtual void Write(IStonReader reader) {
            StonToken token;
            _forceEnd = false;
            while (reader.Read()) {
                if (_forceEnd) break;
                switch ((token = reader.Token).TokenType) {
                    case StonTokenTypes.BeginObject:
                        _node.Count++;
                        _nodeStack.Push(_node = OnBeginObject(_node, token));
                        break;
                    case StonTokenTypes.BeginArray:
                        _node.Count++;
                        _nodeStack.Push(_node = OnBeginArray(_node, token));
                        break;
                    case StonTokenTypes.EndBlock:
                        OnEndBlock(_node);
                        _nodeStack.Pop();
                        if (_nodeStack.Count > 0) {
                            _node = _nodeStack.Peek();
                        } else _node = default(N);
                        break;
                    case StonTokenTypes.Value:
                        _node.Count++;
                        OnValue(_node, token);
                        break;
                    default: throw new NotSupportedException($"Not support Token Type: {token.TokenType}");
                }
            }
        }

        protected abstract N OnBeginObject(N node, StonToken token);
        protected abstract N OnBeginArray(N node, StonToken token);
        protected abstract void OnValue(N node, StonToken token);
        protected abstract void OnEndBlock(N node);

    }

    public class StonNode {
        public int Count { get; internal set; }
        public StonTokenTypes TokenType { get; internal set; }
        public StonNode(StonTokenTypes type = StonTokenTypes.None) {
            TokenType = type;
            Count = 0;
        }
    }
}
