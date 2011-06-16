using System;
using System.Web;
using System.Collections.Generic;

namespace Hyperion.Core.WebSockets
{
    // TODO handle errors
    public class ClientEtiquette
    {
        private static readonly Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Uri uri;
        private readonly string origin;
        private readonly HttpCookieCollection cookies;
        private readonly IDictionary<string, string> extraFields;

        public ClientEtiquette(Uri remoteUri, string origin)
        {
            if (remoteUri == null)
            {
                throw new ArgumentNullException("remoteUri");
            }
            if (string.IsNullOrEmpty(origin))
            {
                throw new ArgumentNullException("origin");
            }
            this.uri = remoteUri;
            this.origin = origin;
        }

        public ClientEtiquette(Uri remoteUri, string origin, HttpCookieCollection cookies, IDictionary<string, string> extraFields)
            : this(remoteUri, origin)
        {
            this.cookies = cookies;
            this.extraFields = extraFields;
        }

        public void GiveHandshake(IWebSocket webSocket, Action handShakenCallback)
        {
            if (webSocket == null || !webSocket.IsConnected || handShakenCallback == null)
            {
                return;
            }

            var resourceName = uri.PathAndQuery;
            var host = uri.WebSocketAuthority();
            var handshake = new ClientHandshake(resourceName, host, origin)
            {
                ExtraFields = extraFields,
                Cookies = cookies
            };
            var state = new GiveHandshakeState
            {
                WebSocket = webSocket,
                Callback = handShakenCallback,
                Handshake = handshake
            };
            var handshakeBuffer = handshake.ToByteArray();
            webSocket.BeginSend(handshakeBuffer, 0, handshakeBuffer.Length, OnGivingHandshake, state);
        }

        private void OnGivingHandshake(IAsyncResult asyncResult)
        {
            var state = (GiveHandshakeState)asyncResult.AsyncState;
            state.WebSocket.EndSend(asyncResult);

            var receivingState = new GivingHandshakeState 
            {
                WebSocket = state.WebSocket,
                Callback = state.Callback,
                Handshake = state.Handshake
            };
            state.WebSocket.BeginReceive(receivingState.Buffer, 0, receivingState.Buffer.Length, OnGivenHandshake, receivingState);
        }

        private void OnGivenHandshake(IAsyncResult asyncResult)
        {
            var state = (GivingHandshakeState)asyncResult.AsyncState;
            var size = state.WebSocket.EndReceive(asyncResult);
            if (size < 1)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("No server handshake data received from " + state.WebSocket.LocalEndPoint);
                state.WebSocket.Dispose();
                return;
            }

            var clientHandshake = state.Handshake;
            var serverHandshake = new ServerHandshake();
            serverHandshake.Parse(state.Buffer, 0, size);
            var expected = serverHandshake.GenerateResponse(clientHandshake.Key1, clientHandshake.Key2, clientHandshake.Key3);
            var location = string.Concat(uri.Scheme, Uri.SchemeDelimiter, clientHandshake.Host, clientHandshake.ResourceName);
            if (serverHandshake.IsValid(location, clientHandshake.Origin, clientHandshake.Subprotocol, expected))
            {
                state.Callback();
            }
            else
            {
                state.WebSocket.Dispose();
            }
        }

        private class GiveHandshakeState
        {
            public IWebSocket WebSocket;
            public Action Callback;
            public ClientHandshake Handshake;
        }

        private class GivingHandshakeState
        {
            private const int BufferSize = 1024;
            public readonly byte[] Buffer = new byte[BufferSize];
            public IWebSocket WebSocket;
            public Action Callback;
            public ClientHandshake Handshake;
        }
    }
}
