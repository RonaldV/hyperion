using System;
using System.Collections.Generic;

namespace Hyperion.Core
{
    public class WebSocketHandlerFactory : IWebSocketHandlerFactory
    {
        private readonly object sync;
        private readonly IDictionary<string, Type> handlersByResourceName;

        public WebSocketHandlerFactory(IDictionary<string, Type> handlersByResourceName)
        {
            sync = new object();
            this.handlersByResourceName = handlersByResourceName;
        }

        public void Add(string resourceName, Type handlerType)
        {
            if (!handlersByResourceName.ContainsKey(resourceName))
            {
                lock (sync)
                {
                    if (!handlersByResourceName.ContainsKey(resourceName))
                    {
                        handlersByResourceName.Add(resourceName, handlerType);
                    }
                }
            }
        }

        public void Remove(string resourceName)
        {
            if (handlersByResourceName.ContainsKey(resourceName))
            {
                lock (sync)
                {
                    if (handlersByResourceName.ContainsKey(resourceName))
                    {
                        handlersByResourceName.Remove(resourceName);
                    }
                }
            }
        }

        private Type Get(string resourceName)
        {
            if (handlersByResourceName.ContainsKey(resourceName))
            {
                lock (sync)
                {
                    if (handlersByResourceName.ContainsKey(resourceName))
                    {
                        return handlersByResourceName[resourceName];
                    }
                }
            }

            return null;
        }

        public IWebSocketHandler Create(string resourceName)
        {
            var handlerType = Get(resourceName);
            if (handlerType == null)
            {
                return null;
            }
            try
            {
                return (IWebSocketHandler)Activator.CreateInstance(handlerType);
            }
            catch
            {
                return null;
            }
        }
    }
}
