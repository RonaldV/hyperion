using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hyperion.Core.WebSockets;
using Hyperion.Samples.Server;
using Hyperion.Core;
using Hyperion.Samples.Common;

namespace Hyperion.Samples.MessagingServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var uri = new Uri("ws://localhost:8000");
            var handlersByResourceName = new Dictionary<string, Type>
            {
                {"/sample", typeof(MessageHandler)}
            };
            var handerFactory = new WebSocketHandlerFactory(handlersByResourceName);
            //var dispatcher = new WebSocketDispatcher(uri, "originTest", handerFactory);
            var etiquette = new ServerEtiquette(uri, "null");
            var dispatcher = new WebSocketDispatcher(uri, etiquette, handerFactory)
            {
                FromFieldName = "From"
            };

            Console.WriteLine("Server started");
            Console.WriteLine("Listening on " + uri);

            var text = string.Empty;
            while ((text = Console.ReadLine()) != "q")
            {
                var post = new Message { From = "Server", Text = text };
                //dispatcher.SendAsync(JsonConvert.SerializeObject(post), "Client");
            }
        }
    }
}
