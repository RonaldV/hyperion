using System;
using Hyperion.Core;
using Hyperion.Core.WebSockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace Hyperion.Samples.Server
{
    /// <summary>
    /// Create self signed certificate with "Visual Studio Command Prompt"
    /// makecert -pe -n "CN=Test And Dev Root Authority" -ss my -sr LocalMachine -a sha1 -sky signature -r "TestDevRootAuthority.cer"
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var serverCertificate = GetServerCertificate();
            var uri = new Uri("ws://localhost:8000");
            var handlersByResourceName = new Dictionary<string, Type>
            {
                {"/sample", typeof(MessageHandler)}
            };
            var handerFactory = new WebSocketHandlerFactory(handlersByResourceName);
            //var dispatcher = new WebSocketDispatcher(uri, "originTest", handerFactory);
            //var dispatcher = new WebSocketDispatcher(uri, "null", handerFactory, serverCertificate)
            var etiquette = new ServerEtiquette(uri);
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
                dispatcher.SendAsync(JsonConvert.SerializeObject(post), "Client");
            }
        }

        private static X509Certificate GetServerCertificate()
        {
            var subjectName = "CN=Test And Dev Root Authority";
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            try
            {
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName,
                subjectName, false);
                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }
            finally 
            {
                store.Close();
            }

            throw new FileNotFoundException(subjectName);

            //var certificatePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            //certificatePath = Path.GetDirectoryName(certificatePath);
            //certificatePath = Path.Combine(certificatePath, "TestDevRootAuthority.cer");
            //var serverCertificate = X509Certificate.CreateFromCertFile(certificatePath);
            //return serverCertificate;
        }
    }
}
