using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Hyperion.Core.WebSockets;
using System.Security.Cryptography.X509Certificates;

namespace Hyperion.Core
{
    public class WebSocketDispatcher : IDisposable
    {
        private static readonly Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IWebSocketListener listener;
        private readonly IWebSocketHandlerFactory handlerFactory;
        private readonly IDictionary<string, List<IWebSocket>> clientsByMetadata;
        private string fromFieldName;

        public WebSocketDispatcher(Uri locationUri,
            string origin,
            IWebSocketHandlerFactory handlerFactory)
            : this(locationUri, origin, handlerFactory, null)
        {
        }

        public WebSocketDispatcher(Uri locationUri,
            string origin,
            IWebSocketHandlerFactory handlerFactory, 
            X509Certificate serverCertificate)
        {
            if (locationUri == null)
            {
                throw new ArgumentNullException("locationUri");
            }
            if (handlerFactory == null)
            {
                throw new ArgumentNullException("handlerFactory");
            }

            this.handlerFactory = handlerFactory;
            clientsByMetadata = new Dictionary<string, List<IWebSocket>>();

            var etiquette = new ServerEtiquette(locationUri, origin);
            listener = new WebSocketListener(locationUri, etiquette, serverCertificate);
            listener.Start(OnHandShaken);
        }

        public string FromFieldName
        {
            set 
            {
                fromFieldName = value;
            }
        }

        //public List<IWebSocket> this[string metadata]
        //{
        //    get
        //    {
        //        if (clientsByMetadata.ContainsKey(metadata))
        //        {
        //            return clientsByMetadata[metadata];
        //        }

        //        return new List<IWebSocket>();
        //    }
        //}

        private void OnHandShaken(IWebSocket webSocket, ClientHandshake clientHandshake)
        {
            var handler = handlerFactory.Create(clientHandshake.ResourceName);
            if (handler != null)
            {
                webSocket.Received = handler.Received;
                webSocket.Disconnected = sender =>
                {
                    var found = clientsByMetadata.FirstOrDefault(kv => kv.Value.Exists(ws => ws == sender));
                    if (found.Value != null)
                    {
                        found.Value.Remove((WebSocket)sender);
                        if (!found.Value.Any())
                        {
                            clientsByMetadata.Remove(found.Key);
                        }
                        // A web socket with the metaData has been disconnected from the server
                        handler.Disconnected(found.Key);
                    }
                };
                webSocket.Error = handler.Error;

                var metadata = GetMetadata(clientHandshake, webSocket);

                // A web socket with the metaData has been connected to the server
                handler.Connected(metadata);

                if (!clientsByMetadata.ContainsKey(metadata))
                {
                    clientsByMetadata.Add(metadata, new List<IWebSocket> { webSocket });
                }
                else
                {
                    //clientsByMetaData[metaData].Dispose();
                    clientsByMetadata[metadata].Add(webSocket);
                }

                // Begin receiving data from the client
                webSocket.ReceiveAsync();
            }
            else
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("There was no handler found for the resource name");
                // If nothing is handling client connections
                // the client connection should be closed
                webSocket.Dispose();
            }
        }

        private string GetMetadata(ClientHandshake clientHandshake, IWebSocket webSocket)
        {
            if (!string.IsNullOrEmpty(fromFieldName) &&
                clientHandshake.ExtraFields != null &&
                clientHandshake.ExtraFields.ContainsKey(fromFieldName))
            {
                return clientHandshake.ExtraFields[fromFieldName];
            }
            else if (!string.IsNullOrEmpty(clientHandshake.Origin) &&
                     !clientHandshake.Origin.Equals("null"))
            {
                return clientHandshake.Origin;
            }
            else
            {
                return webSocket.LocalEndPoint.ToString();
            }
        }

        public void SendAsync(string message, string to)
        {
            if (!clientsByMetadata.ContainsKey(to))
            {
                return;
            }

            clientsByMetadata[to].ForEach(client => client.SendAsync(message));
        }

        public void BroadcastAsync(string message)
        {
            var clientsList = clientsByMetadata.Values;
            foreach (var clients in clientsList)
            {
                clients.ForEach(client => client.SendAsync(message));
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
                    // Dispose managed resources.
                    foreach (var clients in clientsByMetadata.Values)
                    {
                        clients.ForEach(client => client.Dispose());
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
