using System;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Hyperion.Core.WebSockets
{
    // TODO check if you can listen to IPv4 as well as IPv6
    // TODO add possibility to use cookies and add other header fields
    public class WebSocketListener : IWebSocketListener, IDisposable
    {
        private const int DefaultBacklog = 64;
        private static readonly Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Uri locationUri;
        private readonly ServerEtiquette etiquette;
        private readonly X509Certificate serverCertificate;
        private Socket socket;

        /// <summary>
        /// Create new web socket listener
        /// </summary>
        /// <param name="localEndPoint">The local end point address to listen on</param>
        /// <param name="etiquette">Etiquette to use for client-server handshakes</param>
        public WebSocketListener(Uri locationUri, ServerEtiquette etiquette)
        {
            if (locationUri == null)
            {
                throw new ArgumentNullException("locationUri");
            }
            if (etiquette == null)
            {
                throw new ArgumentNullException("etiquette");
            }

            this.etiquette = etiquette;
            this.locationUri = locationUri;
        }

        public WebSocketListener(Uri locationUri, ServerEtiquette etiquette, X509Certificate serverCertificate)
            : this(locationUri, etiquette)
        {
            if (serverCertificate == null && locationUri.Scheme == UriWeb.UriSchemeWss)
            {
                throw new ArgumentNullException("serverCertificate");
            }
            this.serverCertificate = serverCertificate;
        }

        public bool ClientCertificateRequired { get; set; }
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Start listening
        /// </summary>
        /// <param name="handShakenCallback">Callback when handshake was successful</param>
        public void Start(Action<IWebSocket, ClientHandshake> handShakenCallback)
        {
            if (handShakenCallback == null)
            {
                throw new ArgumentNullException("handShakenCallback");
            }

            var localEndPoint = new IPEndPoint(IPAddress.Any, locationUri.Port);
            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.IP);
            socket.Bind(localEndPoint);
            socket.Listen(DefaultBacklog);
            socket.BeginAccept(OnClientConnect, handShakenCallback);
        }

        /// <summary>
        /// Processes client connections
        /// </summary>
        /// <param name="asyncResult">Async result containing the connecting client socket</param>
        private void OnClientConnect(IAsyncResult asyncResult)
        {
            var handShakenCallback = (Action<IWebSocket, ClientHandshake>)asyncResult.AsyncState;
            var clientSocket = socket.SafeEndAccept(asyncResult);
            var clientWebSocket = new WebSocket(clientSocket);
            if (locationUri.Scheme == UriWeb.UriSchemeWss)
            {
                var sslStream = clientWebSocket.CreateSslStream(null);
                var state = new AuthenticateAsServerState 
                { 
                    WebSocket = clientWebSocket, 
                    SslStream = sslStream,
                    HandShakenCallback = handShakenCallback
                };
                sslStream.BeginAuthenticateAsServer(serverCertificate,
                    ClientCertificateRequired,
                    SslProtocols.Tls,
                    CheckCertificateRevocation,
                    OnAuthenticateAsServer,
                    state);
            }
            else
            {
                etiquette.ReceiveHandshake(clientWebSocket, handShakenCallback);
            }

            // Listen for the next client connection
            socket.BeginAccept(OnClientConnect, handShakenCallback);
        }

        private void OnAuthenticateAsServer(IAsyncResult asyncResult)
        {
            var webSocket = default(IWebSocket);
            try
            {
                var state = (AuthenticateAsServerState)asyncResult.AsyncState;
                webSocket = state.WebSocket;
                state.SslStream.EndAuthenticateAsServer(asyncResult);

                etiquette.ReceiveHandshake(state.WebSocket, state.HandShakenCallback);
            }
            catch (Exception)
            {
                if (webSocket != null)
                {
                    webSocket.Dispose();
                }
            }
        }

        private class AuthenticateAsServerState
        {
            public IWebSocket WebSocket;
            public System.Net.Security.SslStream SslStream;
            public Action<IWebSocket, ClientHandshake> HandShakenCallback;
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
                    if (socket != null)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
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
