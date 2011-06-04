using System;
using System.Net.Sockets;

namespace Hyperion.Core.WebSockets
{
    public interface IWebSocketListener
    {
        void Start(Action<IWebSocket, ClientHandshake> handShakenWithClientCallback);
    }
}
