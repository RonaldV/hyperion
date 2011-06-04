using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using System;
using Hyperion.Messages;

namespace Hyperion.Messaging
{
    public class BinaryMessageFormatter : IMessageFormatter
    {
        private IFormatter formatter;

        public BinaryMessageFormatter()
        {
            formatter = new BinaryFormatter();
        }

        public string Serialize<T>(T message)
             where T : IMessage
        {
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, message);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public T Deserialize<T>(string serialisedObject)
             where T : IMessage
        {
            var bytes = Convert.FromBase64String(serialisedObject);
            using (var stream = new MemoryStream(bytes))
            {
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
