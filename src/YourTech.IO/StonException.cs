using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourTech.IO {
    public class StonException : Exception {
        public StonException(string message = null, Exception innerException = null) : base(message, innerException) { }
    }
}
