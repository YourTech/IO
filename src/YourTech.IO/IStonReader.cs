using System;

namespace YourTech.IO {
    public interface IStonReader : IDisposable {
        int Level { get; }
        int LevelIndex { get; }

        StonToken BlockToken { get; }
        StonToken Token { get; }
        bool CanRead { get; }

        bool Read();
    }
}
