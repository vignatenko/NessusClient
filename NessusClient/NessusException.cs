using System;
using System.Runtime.Serialization;

namespace NessusClient
{
    [Serializable]
    internal class NessusException : Exception
    {
        public NessusException()
        {
        }

        public NessusException(string message) : base(message)
        {
        }

        public NessusException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NessusException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}