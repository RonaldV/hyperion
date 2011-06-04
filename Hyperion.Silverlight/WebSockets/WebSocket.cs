using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Text;

namespace Hyperion.Silverlight.WebSockets
{
    // TODO handle errors
    public class WebSocket : IWebSocket, IDisposable
    {
        private const int BufferSize = 1024;

        /// <summary>
        /// Create a new web socket
        /// </summary>
        /// <param name="socket">Socket to use in the socket</param>
        public WebSocket(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            Socket = socket;
        }

        public Socket Socket { get; private set; }
        public Action<string> Connected { get; set; }
        public Action<string> Received { get; set; }
        public Action<IWebSocket> Disconnected { get; set; }

        private void RaiseConnected(string data)
        {
            if (Connected != null && !string.IsNullOrEmpty(data))
            {
                Connected(data);
            }
        }

        private void RaiseReceived(string data)
        {
            if (Received != null && !string.IsNullOrEmpty(data))
            {
                Received(data);
            }
        }

        private void RaiseDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this);
            }
        }

        /// <summary>
        /// Connects to the remote uri
        /// </summary>
        /// <param name="uri">Uri of the remote location.</param>
        /// <param name="connectedCallback">Callback for when the connection succeeded.</param>
        public void Connect(Uri uri, Action connectedCallback)
        {
            var host = uri.DnsSafeHost;
            var port = uri.WebSocketPort();

            var remoteEndPoint = new DnsEndPoint(host, port);

            var onCompleted = default(EventHandler<SocketAsyncEventArgs>);
            onCompleted = new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                e.Completed -= onCompleted;

                if (e.SocketError != SocketError.Success)
                {
                    // TODO handle error
                    return;
                }

                connectedCallback();
                RaiseConnected(uri.ToString());
            });
            var args = new SocketAsyncEventArgs();
            args.UserToken = Socket;
            args.RemoteEndPoint = remoteEndPoint;
            args.Completed += onCompleted;

            Socket.ConnectAsync(args);
        }

        /// <summary>
        /// Send data asynchronous
        /// </summary>
        /// <param name="data">Data to send</param>
        public void SendAsync(string data)
        {
            if (Socket.Connected)
            {
                var buffer = new Frame(data).ToByteArray();
                var args = new SocketAsyncEventArgs();
                args.SetBuffer(buffer, 0, buffer.Length);
                args.UserToken = Socket;
                args.RemoteEndPoint = Socket.RemoteEndPoint;

                Socket.SendAsync(args);
            }
            else
            {
                Dispose();
            }
        }

        /// <summary>
        /// Receive data asynchronous
        /// </summary>
        public void ReceiveAsync()
        {
            var buffer = new byte[BufferSize];
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, 0, buffer.Length);
            args.UserToken = new Frame();
            args.RemoteEndPoint = Socket.RemoteEndPoint;
            args.Completed += OnReceiveCompleted;

            ReceiveAsync(args);
        }

        private void ReceiveAsync(SocketAsyncEventArgs args)
        {
            if (Socket == null || !Socket.Connected)
            {
                RaiseDisconnected();
                return;
            }
            Socket.ReceiveAsync(args);
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnReceiveCompleted;

            if (e.SocketError != SocketError.Success)
            {
                Dispose();
                return;
            }
            if (e.BytesTransferred < 1)
            {
                Dispose();
                return;
            }

            var frame = (Frame)e.UserToken;
            frame.Add(e.Buffer); // TODO maybe add index and count to remove unneeded bytes
            if (frame.IsClosed)
            {
                RaiseReceived(frame.ToContentString());
                // Begin receiving data for a new frame
                ReceiveAsync();
            }
            else
            {
                // Receive more data for the current frame
                var buffer = new byte[BufferSize];
                e.SetBuffer(buffer, 0, buffer.Length);
                e.Completed += OnReceiveCompleted;
                ReceiveAsync(e);
            }
        }

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    RaiseDisconnected();
                    // Dispose managed resources.
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Close();
                    Received = null;
                    Disconnected = null;
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                disposed = true;
            }
        }

        #endregion
    }
}
