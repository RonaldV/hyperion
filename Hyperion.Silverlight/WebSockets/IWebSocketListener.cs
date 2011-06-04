using System;
using System.Net.Sockets;

namespace Hyperion.Silverlight.WebSockets
{
    public interface IWebSocketListener
    {
        void Start(Action<Socket, ClientHandshake> handShakenWithClientCallback);
    }
}
