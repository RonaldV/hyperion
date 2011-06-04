using Hyperion.Messages;

namespace Hyperion.Messaging
{
    public interface IMessageFormatter
    {
        string Serialize<T>(T message) where T : IMessage;
        T Deserialize<T>(string serialisedObject) where T : IMessage;
    }
}
