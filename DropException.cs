using System;
using System.Runtime.Serialization;

namespace DropDownloadCore
{
    public class DropException : Exception
    {
        public DropException() { }

        public DropException(string message) : base(message) { }

        public DropException(string message, Exception innerException) : base(message, innerException) { }

        protected DropException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}