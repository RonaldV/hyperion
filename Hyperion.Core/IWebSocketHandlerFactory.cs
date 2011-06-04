namespace Hyperion.Core
{
    public interface IWebSocketHandlerFactory
    {
        IWebSocketHandler Create(string resourceName);
    }
}
