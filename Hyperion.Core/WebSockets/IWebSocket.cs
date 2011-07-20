using System;
using System.Net;

namespace Hyperion.Core.WebSockets
{
    public interface IWebSocket : IDisposable
    {
        bool IsConnected { get; }
        EndPoint LocalEndPoint { get; }

        Action<string> Received { set; }
        Action<IWebSocket> Disconnected { set; }
        Action<Exception> Error { set; }

        void BeginSend(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        void EndSend(IAsyncResult asyncResult);
        void SendAsync(string data);
        void BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback, object state);
        int EndReceive(IAsyncResult asyncResult);
        void ReceiveAsync();
    }
}
