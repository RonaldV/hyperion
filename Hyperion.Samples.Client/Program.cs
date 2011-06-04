using System;
using System.Collections.Generic;
using Hyperion.Core;
using Hyperion.Core.WebSockets;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Hyperion.Samples.Client
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

            //var webSocketClient = new WebSocketClient(uri, etiquette, handler, OnCertificateValidation);
            var webSocketClient = new WebSocketClient(uri, etiquette, handler);
            webSocketClient.Connect();

            Console.WriteLine("Client started");
            Console.WriteLine("Connecting to " + uri);

            var text = string.Empty;
            while ((text = Console.ReadLine()) != "q")
            {
                var message = new Message { From = "Client", Text = text };
                webSocketClient.SendAsync(JsonConvert.SerializeObject(message));
            }
        }

        private static bool OnCertificateValidation(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            //if (sslPolicyErrors != SslPolicyErrors.None)
            //{
            //    return false;
            //}

            //return true;

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                // Ignore certificate errors
                Console.WriteLine("Ignore certificate errors: {0}", sslPolicyErrors);

                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                {
                    foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                    {
                        Console.WriteLine("\t" + chainStatus.Status);
                    }
                }
            }

            return true;
        }
    }
}
