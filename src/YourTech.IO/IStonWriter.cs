using System;

namespace YourTech.IO {
    public interface IStonWriter : IDisposable {
        StonNode Node { get; }
        void Write(IStonReader reader);
        void Write(object value, string propertyName = null);
    }
}