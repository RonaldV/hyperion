using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;

namespace Hyperion.Core.WebSockets
{
    // TODO Add compression
    public class WebSocket : IWebSocket, IDisposable
    {
        private readonly object sync;
        private Socket socket;
        private Stream stream;
        private Queue<string> sendQueue;
        private bool isSending;

        /// <summary>
        /// Create a new unconnected web socket
        /// </summary>
        public WebSocket()
        {
            socket = CreateSocket();
            sync = new object();
            sendQueue = new Queue<string>();
            isSending = false;
        }

        /// <summary>
        /// Create a new connected web socket
        /// </summary>
        /// <param name="socket">The connected socket to use</param>
        public WebSocket(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            if (!socket.Connected)
            {
                throw new ArgumentException("socket is not connected");
            }

            this.socket = socket;
            stream = new NetworkStream(socket);
            sync = new object();
            sendQueue = new Queue<string>();
            isSending = false;
        }

        private Socket CreateSocket()
        {
            //if (Socket.OSSupportsIPv6)
            //{
            //    return new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.IP);
            //}

            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        public bool IsConnected { get { return socket != null && socket.Connected; } }
        public EndPoint LocalEndPoint { get { return socket.LocalEndPoint; } }

        public Action<string> Connected { private get; set; }
        public Action<string> Received { private get; set; }
        public Action<IWebSocket> Disconnected { private get; set; }
        public Action<Exception> Error { private get; set; }

        /// <summary>
        /// Connects to the remote uri
        /// </summary>
        /// <param name="uri">Uri of the remote location.</param>
        /// <param name="connectedCallback">Callback for when the connection succeeded.</param>
        public void Connect(Uri uri, Action connectedCallback) //X509CertificateCollection clientCertificates
        {
            var host = uri.DnsSafeHost;
            var port = uri.WebSocketPort();

            var hostEntry = Dns.GetHostEntry(host);
            var ipAddress = hostEntry.AddressList.First(a => a.AddressFamily == socket.AddressFamily);
            var remoteEndPoint = new IPEndPoint(ipAddress, port);

            socket.BeginConnect(remoteEndPoint, asyncResult =>
            {
                socket.EndConnect(asyncResult, Error.Raise);
                if (socket.Connected)
                {
                    stream = new NetworkStream(socket);
                    connectedCallback();
                    Connected.Raise(uri.ToString());
                }
            }, null);
        }

        public SslStream CreateSslStream(RemoteCertificateValidationCallback certificateValidationCallback)
        {
            var leaveInnerStreamOpen = false;
            var sslStream = default(SslStream);
            if (certificateValidationCallback == null)
            {
                sslStream = new SslStream(stream, leaveInnerStreamOpen);
            }
            else
            {
                sslStream = new SslStream(stream, leaveInnerStreamOpen, certificateValidationCallback);
            }
            stream = sslStream;

            return sslStream;
        }

        public void BeginSend(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (socket.Connected)
            {
                stream.BeginWrite(buffer, offset, count, callback, state);
            }
            else
            {
                Dispose();
            }
        }

        public void EndSend(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult, Error.Raise);
        }

        /// <summary>
        /// Send data asynchronous
        /// </summary>
        /// <param name="data">Data to send</param>
        public void SendAsync(string data)
        {
            if (data == null)
            {
                return;
            }

            var isCurrentlySending = true;
            lock (sync)
            {
                sendQueue.Enqueue(data);
                isCurrentlySending = isSending;
                isSending = true;
            }

            if (!isCurrentlySending)
            {
                SendNext();
            }
        }

        private void SendNext()
        {
            var data = default(string);
            lock (sync)
            {
                if (sendQueue.Count < 1)
                {
                    isSending = false;
                    return;
                }
                data = sendQueue.Dequeue();
            }

            if (socket.Connected)
            {
                var buffer = new Frame(data).ToByteArray();
                stream.BeginWrite(buffer, 0, buffer.Length, OnSendNext, null);
            }
            else
            {
                Dispose();
            }
        }

        private void OnSendNext(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult, Error.Raise);
            SendNext();
        }

        public void BeginReceive(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (socket.Connected)
            {
                stream.BeginRead(buffer, offset, count, callback, state);
            }
            else
            {
                Dispose();
            }
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return stream.EndRead(asyncResult, Error.Raise);
        }

        /// <summary>
        /// Receive data asynchronous
        /// </summary>
        public void ReceiveAsync()
        {
            var state = new ReceiveState
            {
                //Socket = socket,
                Frame = new Frame()
            };
            ReceiveAsync(state);
        }

        private void ReceiveAsync(ReceiveState state)
        {
            //var socket = state.Socket;
            if (socket == null || !socket.Connected)
            {
                Disconnected.Raise(this);
                return;
            }
            stream.BeginRead(state.Buffer, 0, state.Buffer.Length, OnReceiveAsync, state);
        }

        private void OnReceiveAsync(IAsyncResult asyncResult)
        {
            var state = (ReceiveState)asyncResult.AsyncState;
            //var size = state.Socket.SafeEndReceive(asyncResult);
            var size = stream.EndRead(asyncResult, Error.Raise);
            if (size < 1)
            {
                // No data was received
                Dispose();
            }

            var frame = state.Frame;
            frame.Add(state.Buffer);
            if (frame.IsClosed)
            {
                Received.Raise(frame.ToContentString());
                // Begin receiving data for a new frame
                ReceiveAsync();
            }
            else
            {
                // Receive more data for the current frame
                var nextState = new ReceiveState
                {
                    //Socket = state.Socket,
                    Frame = state.Frame
                };
                ReceiveAsync(nextState);
            }
        }

        private class ReceiveState
        {
            private const int BufferSize = 1024;
            public readonly byte[] Buffer = new byte[BufferSize];
            //public Socket Socket;
            public Frame Frame;
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
                    Disconnected.Raise(this);

                    // Dispose managed resources.
                    lock (sync)
                    {
                        isSending = false;
                        sendQueue.Clear();
                    }

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    stream.Close();
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
