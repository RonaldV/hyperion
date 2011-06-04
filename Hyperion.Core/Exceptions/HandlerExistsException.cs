using System;

namespace Hyperion.Core.Exceptions
{
    [Serializable]
    public class HandlerExistsException : Exception
    {
        public HandlerExistsException()
        { 
        }

        public HandlerExistsException(string message)
            : base(message)
        { 
        }

        public HandlerExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public HandlerExistsException(System.Runtime.Serialization.SerializationInfo info, 
                                      System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
