using System;

namespace Hyperion.Core.WebSockets
{
    // TODO handle errors

    /// <summary>
    /// Etiquette for the handshake between the server and the client
    /// </summary>
    public class ServerEtiquette
    {
        private static readonly Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string origin;
        private readonly string host;
        private readonly string scheme;

        public ServerEtiquette(Uri locationUri, string origin)
        {
            if (locationUri == null)
            {
                throw new ArgumentNullException("locationUri");
            }
            if (string.IsNullOrEmpty(origin))
            {
                throw new ArgumentNullException("origin");
            }
            this.origin = origin;
            this.host = locationUri.WebSocketAuthority();
            this.scheme = locationUri.Scheme;
        }

        /// <summary>
        /// Receive handshake from the connected socket
        /// </summary>
        /// <param name="webSocket">The connected socket</param>
        /// <param name="handShakenCallback">Callback when the handshake was successful</param>
        public void ReceiveHandshake(IWebSocket webSocket, Action<IWebSocket, ClientHandshake> handShakenCallback)
        {
            if (webSocket == null || !webSocket.IsConnected || handShakenCallback == null)
            {
                return;
            }

            var state = new ReceiveHandshakeState
                            {
                                WebSocket = webSocket,
                                Callback = handShakenCallback
                            };
            webSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, OnReceivingHandshake, state);
        }

        private void OnReceivingHandshake(IAsyncResult asyncResult)
        {
            var state = (ReceiveHandshakeState)asyncResult.AsyncState;
            var size = state.WebSocket.EndReceive(asyncResult);
            if (size < 1)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("No client handshake data received from " + state.WebSocket.LocalEndPoint);
                state.WebSocket.Dispose();
                return;
            }

            var clientHandshake = new ClientHandshake();
            clientHandshake.Parse(state.Buffer, 0, size);
            if (clientHandshake.IsValid(host, origin))
            {
                var serverHandshake = new ServerHandshake(clientHandshake.Origin,
                    string.Concat(scheme, Uri.SchemeDelimiter, clientHandshake.Host, clientHandshake.ResourceName),
                    clientHandshake.Subprotocol,
                    clientHandshake.Key1, clientHandshake.Key2, clientHandshake.Key3);
                
                ReturnHandshake(serverHandshake, clientHandshake, state);
            }
            else
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Invalid client handshake from " + state.WebSocket.LocalEndPoint);
                state.WebSocket.Dispose();
            }
        }

        private void ReturnHandshake(ServerHandshake serverHandshake, ClientHandshake clientHandshake, ReceiveHandshakeState handshakeState)
        {
            var webSocket = handshakeState.WebSocket;
            var state = new ReturnHandshakeState { WebSocket = webSocket, Callback = handshakeState.Callback, ClientHandshake = clientHandshake };
            var handshakeBuffer = serverHandshake.ToByteArray();
            webSocket.BeginSend(handshakeBuffer, 0, handshakeBuffer.Length, OnHandshakeReturned, state);
        }

        protected virtual void OnHandshakeReturned(IAsyncResult ar)
        {
            var state = (ReturnHandshakeState)ar.AsyncState;
            state.WebSocket.EndSend(ar);
            state.Callback(state.WebSocket, state.ClientHandshake);
        }

        private class ReceiveHandshakeState
        {
            private const int BufferSize = 1024;
            public readonly byte[] Buffer = new byte[BufferSize];
            public IWebSocket WebSocket;
            public Action<IWebSocket, ClientHandshake> Callback;
        }

        private class ReturnHandshakeState
        {
            public IWebSocket WebSocket;
            public Action<IWebSocket, ClientHandshake> Callback;
            public ClientHandshake ClientHandshake;
        }
    }
}
