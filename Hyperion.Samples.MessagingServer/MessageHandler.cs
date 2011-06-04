using System;
using Hyperion.Core;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using Hyperion.Messaging;
using Hyperion.Samples.Common;

namespace Hyperion.Samples.Server
{
    public class MessageHandler : IWebSocketHandler
    {
        public void Connected(string data)
        {
            Console.WriteLine("Connected to " + data);
        }

        public void Received(string data)
        {
            try
            {
                var formatter = new BinaryMessageFormatter();
                var message = formatter.Deserialize<Message>(data);
                Console.WriteLine("{0}: {1}", message.From, message.Text);

                //byte[] b = System.Text.Encoding.UTF8.GetBytes(data);
                //using (MemoryStream ms = new MemoryStream(b))
                //{
                //    var message = formatter.Deserialize(ms);
                //    //var message = (Message)formatter.Deserialize(stream.BaseStream);
                //    //Console.WriteLine("{0}: {1}", message.From, message.Text);
                //}
                //var message = JsonConvert.DeserializeObject<Message>(data);
                //Console.WriteLine("{0}: {1}", message.From, message.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
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