using System;
using Hyperion.Core.WebSockets;

namespace Hyperion.Messaging.Builders
{
    // TODO get the builder to build the client or server too
    // in case of server ... filequeue adjustment
    public class MessageBusBuilder
    {
        private IWebSocket webSocket;
        private IFileQueue fileQueue;
        private IMessageFormatter messageFormatter;

        public MessageBusBuilder UsingTransport(IWebSocket webSocket)
        {
            this.webSocket = webSocket;
            return this;
        }

        public MessageBusBuilder PersistingIn(IFileQueue fileQueue)
        {
            this.fileQueue = fileQueue;
            return this;
        }

        public MessageBusBuilder WithFormat(IMessageFormatter messageFormatter)
        {
            this.messageFormatter = messageFormatter;
            return this;
        }

        public MessageBus Start()
        {
            if (webSocket == null)
            {
                throw new NullReferenceException();
            }
            if (fileQueue == null)
            {
                fileQueue = new FileQueue();
            }
            if (messageFormatter == null)
            {
                messageFormatter = new BinaryMessageFormatter();
            }
            return new MessageBus(webSocket, fileQueue, messageFormatter);
        }

        public static implicit operator MessageBus(MessageBusBuilder builder)
        {
            return builder.Start();
        }
    }

    //public class MessageBusBuilder
    //{
    //    private IWebSocket webSocket;
    //    private IFileQueue fileQueue;
    //    private IMessageFormatter messageFormatter;

    //    public MessageBusBuilder UsingTransport(IWebSocket webSocket)
    //    {
    //        this.webSocket = webSocket;
    //        return this;
    //    }

    //    public MessageBusBuilder PersistingIn(IFileQueue fileQueue)
    //    {
    //        this.fileQueue = fileQueue;
    //        return this;
    //    }

    //    public MessageBusBuilder WithFormat(IMessageFormatter messageFormatter)
    //    {
    //        this.messageFormatter = messageFormatter;
    //        return this;
    //    }

    //    public MessageBus Build()
    //    {
    //        if (fileQueue == null)
    //        {
    //            fileQueue = new FileQueue();
    //        }
    //        if (messageFormatter == null)
    //        {
    //            messageFormatter = new BinaryMessageFormatter();
    //        }
    //        return new MessageBus(webSocket, fileQueue, messageFormatter);
    //    }

    //    public static implicit operator MessageBus(MessageBusBuilder builder)
    //    {
    //        return builder.Build();
    //    }
    //}
}
