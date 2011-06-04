using System;
using System.Collections.Generic;
using System.Linq;
using Hyperion.Core.WebSockets;
using Hyperion.Messages;

namespace Hyperion.Messaging
{
    /// <summary>
    /// TODO Transaction support
    /// </summary>
    public class MessageBus
    {
        private readonly IWebSocket webSocket;
        private readonly MessageQueue messageQueue;
        private readonly IMessageFormatter formatter;
        //private readonly IDictionary<string, List<IWebSocket>> clientsByMetadata;
        //private readonly IDictionary<string, MessageQueue> messageQueuesByMetadata;

        public MessageBus(IWebSocket webSocket, IFileQueue fileQueue, IMessageFormatter formatter)
        {
            if (webSocket == null)
            {
                throw new ArgumentNullException("webSocket");
            }
            if (fileQueue == null)
            {
                throw new ArgumentNullException("fileQueue");
            }
            if (formatter == null)
            {
                throw new ArgumentNullException("formatter");
            }
            this.webSocket = webSocket;
            this.formatter = formatter;
            messageQueue = new MessageQueue(fileQueue);
        }

        //public void Register(string metadata, IWebSocket webSocket)
        //{
        //    if (clientsByMetadata.ContainsKey(metadata))
        //    {
        //        clientsByMetadata[metadata].Add(webSocket);
        //    }
        //    else 
        //    {
        //        clientsByMetadata.Add(metadata, new List<IWebSocket> { webSocket });
        //        messageQueuesByMetadata.Add(metadata, new FileQueue(metadata));
        //    }
        //}

        /// <summary>
        /// Sends reliable message
        /// </summary>
        /// <param name="message">Message to send</param>
        public void Send(IMessage message)
        {
            if (message == null)
            {
                return;
            }

            var serializedMessage = formatter.Serialize(message);
            messageQueue.Enqueue(serializedMessage);
            Send();
        }

        /// <summary>
        /// Sends all unsent messages
        /// </summary>
        public void Send()
        {
            var unsentFileNames = new List<string>();
            try
            {
                var fileNames = messageQueue.DequeueAllFileNames();
                unsentFileNames = fileNames.ToList();
                foreach (var fileName in fileNames)
                {
                    using (var fileMessage = messageQueue.GetMessage(fileName))
                    {
                        webSocket.SendAsync(fileMessage.Message);
                    }
                    unsentFileNames.Remove(fileName);
                }
            }
            catch (Exception) // TODO check which exceptions can happen
            {
                messageQueue.RequeueAllFileNames(unsentFileNames);
            }
        }
    }
}
