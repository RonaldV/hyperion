using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Hyperion.Silverlight.WebSockets
{
    public class ClientEtiquette
    {
        private const int BufferSize = 1024;
        private readonly Uri uri;
        private readonly string origin;
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

        public ClientEtiquette(Uri remoteUri, string origin, IDictionary<string, string> extraFields)
            : this(remoteUri, origin)
        {
            this.extraFields = extraFields;
        }

        public void GiveHandshake(Socket socket, Action handShakenCallback)
        {
            if (socket == null || !socket.Connected || handShakenCallback == null)
            {
                return;
            }

            var resourceName = uri.AbsolutePath; // TODO check ... PathAndQuery
            var host = uri.WebSocketAuthority();
            var handshake = new ClientHandshake(resourceName, host, origin)
            {
                ExtraFields = extraFields
            };
            var token = new HandshakeToken
            {
                Socket = socket,
                Callback = handShakenCallback,
                Handshake = handshake
            };

            var handshakeBuffer = handshake.ToByteArray();
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(handshakeBuffer, 0, handshakeBuffer.Length);
            args.UserToken = token;
            args.RemoteEndPoint = socket.RemoteEndPoint;
            args.Completed += OnGivingHandshakeCompleted;

            socket.SendAsync(args);
        }

        private void OnGivingHandshakeCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnGivingHandshakeCompleted;

            var token = (HandshakeToken)e.UserToken;
            if (e.SocketError != SocketError.Success)
            {
                token.Socket.Shutdown(SocketShutdown.Both);
                token.Socket.Close();
                return;
            }
            if (e.BytesTransferred < 1)
            {
                token.Socket.Shutdown(SocketShutdown.Both);
                token.Socket.Close();
                return;
            }

            var buffer = new byte[BufferSize];
            e.SetBuffer(buffer, 0, buffer.Length);
            e.Completed += OnGivenHandshakeCompleted;

            token.Socket.ReceiveAsync(e);
        }

        private void OnGivenHandshakeCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= OnGivenHandshakeCompleted;

            var token = (HandshakeToken)e.UserToken;
            if (e.SocketError != SocketError.Success)
            {
                token.Socket.Shutdown(SocketShutdown.Both);
                token.Socket.Close();
                return;
            }
            if (e.BytesTransferred < 1)
            {
                token.Socket.Shutdown(SocketShutdown.Both);
                token.Socket.Close();
                return;
            }

            var clientHandshake = token.Handshake;
            var serverHandshake = new ServerHandshake();
            serverHandshake.Parse(e.Buffer, 0, e.BytesTransferred);
            var expected = serverHandshake.GenerateResponse(clientHandshake.Key1, clientHandshake.Key2, clientHandshake.Key3);
            var location = string.Concat(uri.Scheme, Uri.SchemeDelimiter, clientHandshake.Host, clientHandshake.ResourceName);
            if (serverHandshake.IsValid(location, clientHandshake.Origin, clientHandshake.Subprotocol, expected))
            {
                token.Callback();
            }
            else
            {
                token.Socket.Shutdown(SocketShutdown.Both);
                token.Socket.Close();
            }
        }

        private class HandshakeToken
        {
            public Socket Socket;
            public Action Callback;
            public ClientHandshake Handshake;
        }
    }
}
