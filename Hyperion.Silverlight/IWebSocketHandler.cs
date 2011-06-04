namespace Hyperion.Silverlight
{
    public interface IWebSocketHandler
    {
        // TODO maybe add websocket when connected
        void Connected(string data);
        void Received(string data);
        void Disconnected(string data);
    }
}
