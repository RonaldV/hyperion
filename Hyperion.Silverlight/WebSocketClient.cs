using System;
using System.Net;
using System.Net.Sockets;
using Hyperion.Silverlight.WebSockets;

namespace Hyperion.Silverlight
{
    #region TODO implement TLS
    //If /secure/ is true, perform a TLS handshake over the
    //connection.  If this fails (e.g. the server's certificate could
    //not be verified), then fail the WebSocket connection and abort
    //these steps.  Otherwise, all further communication on this
    //channel must run through the encrypted tunnel.  [RFC2246]

    //User agents must use the Server Name Indication extension in the
    //TLS handshake.  [RFC4366]
    #endregion

    #region TODO send close connection to server
    //Once a WebSocket connection is established, the user agent must use
    //the following steps to *start the WebSocket closing handshake*.
    //These steps must be run asynchronously relative to whatever algorithm
    //invoked this one.

    //1.  If the WebSocket closing handshake has started, then abort these
    //    steps.

    //2.  Send a 0xFF byte to the server.

    //3.  Send a 0x00 byte to the server.

    //4.  *The WebSocket closing handshake has started*.

    //5.  Wait a user-agent-determined length of time, or until the
    //    WebSocket connection is closed.

    //6.  If the WebSocket connection is not already closed, then close the
    //    WebSocket connection.  (If this happens, then the closing
    //    handshake doesn't finish.)

    //NOTE: The closing handshake finishes once the server returns the 0xFF
    //packet, as described above.
    #endregion

    // TODO only one socket connection to server. A client dispatcher?
    public class WebSocketClient : IDisposable
    {
        private const string UnsupportedSchemeExceptionMessage = "Unsupported scheme ";
        private readonly Uri uri;
        private readonly WebSocket webSocket;
        private readonly ClientEtiquette etiquette;

        public WebSocketClient(Uri remoteUri, ClientEtiquette etiquette)
        {
            if (remoteUri == null)
            {
                throw new ArgumentNullException("remoteUri");
            }
            if (etiquette == null)
            {
                throw new ArgumentNullException("etiquette");
            }
            if (!remoteUri.Scheme.Equals(UriWeb.UriSchemeWs) &&
                !remoteUri.Scheme.Equals(UriWeb.UriSchemeWss))
            {
                throw new ArgumentException(string.Concat(UnsupportedSchemeExceptionMessage, remoteUri.Scheme));
            }

            this.uri = remoteUri;
            this.etiquette = etiquette;
            webSocket = new WebSocket(CreateSocket());
        }

        public WebSocketClient(Uri uri, ClientEtiquette etiquette, IWebSocketHandler handler)
            : this(uri, etiquette)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            webSocket.Connected = handler.Connected;
            webSocket.Received = handler.Received;
            webSocket.Disconnected = sender => handler.Disconnected(uri.ToString());
        }

        private Socket CreateSocket()
        {
            if (Socket.OSSupportsIPv6)
            {
                return new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            }

            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect()
        {
            webSocket.Connect(uri, () =>
                etiquette.GiveHandshake(webSocket.Socket, () =>
                        webSocket.ReceiveAsync()));
        }

        public void SendAsync(string data)
        {
            webSocket.SendAsync(data);
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
                    // Dispose managed resources.
                    webSocket.Dispose();
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
