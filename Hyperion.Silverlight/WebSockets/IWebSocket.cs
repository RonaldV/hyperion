using System;

namespace Hyperion.Silverlight.WebSockets
{
    public interface IWebSocket
    {
        Action<string> Received { get; set; }
        Action<IWebSocket> Disconnected { get; set; }

        void SendAsync(string data);
        void ReceiveAsync();
    }
}
