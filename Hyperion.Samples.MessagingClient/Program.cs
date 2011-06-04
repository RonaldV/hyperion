using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hyperion.Core;
using Hyperion.Samples.Client;
using Hyperion.Core.WebSockets;
using Hyperion.Messaging;
using Hyperion.Samples.Common;
using Hyperion.Messaging.Builders;

namespace Hyperion.Samples.MessagingClient
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.Sleep(1000);

            var uri = new Uri("ws://localhost:8000/sample");
            var handler = new MessageHandler();
            var etiquette = new ClientEtiquette(uri, "null", null, new Dictionary<string, string>
                                        {
                                            {"From", "Client"}
                                        });
            //var etiquette = new ClientEtiquette(uri, "originTest");
            var webSocketClient = new WebSocketClient(uri, etiquette, handler);
            webSocketClient.Connect();

            var messageBus = Configure.Bus
                .UsingTransport(webSocketClient.WebSocket)
                .Start();

            Console.WriteLine("Client started");
            Console.WriteLine("Connecting to " + uri);

            var text = string.Empty;
            while ((text = Console.ReadLine()) != "q")
            {
                var message = new Message { From = "Client", Text = text };
                messageBus.Send(message);
            }
        }
    }
}
