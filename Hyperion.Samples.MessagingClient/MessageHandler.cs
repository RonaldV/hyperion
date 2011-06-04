using System;
using Hyperion.Core;

namespace Hyperion.Samples.Client
{
    public class MessageHandler : IWebSocketHandler
    {
        public void Connected(string data)
        {
            Console.WriteLine("Connected to " + data);
        }

        public void Received(string data)
        {
            //var message = JsonConvert.DeserializeObject<Message>(data);
            //Console.WriteLine("{0}: {1}", message.From, message.Text);
        }

        public void Disconnected(string data)
        {
            Console.WriteLine("Disconnected from " + data);
        }

        public void Error(Exception ex)
        {
            Console.WriteLine("Error {0}", ex);
        }
    }
}