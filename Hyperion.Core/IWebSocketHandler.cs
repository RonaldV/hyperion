namespace Hyperion.Core
{
    public interface IWebSocketHandler
    {
        // TODO maybe add websocket when connected
        void Connected(string data);
        void Received(string data);
        void Disconnected(string data);
        void Error(System.Exception ex);
    }
}
