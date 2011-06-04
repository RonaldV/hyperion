﻿using System;
using Hyperion.Core;
using Newtonsoft.Json;

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
                var message = JsonConvert.DeserializeObject<Message>(data);
                Console.WriteLine("{0}: {1}", message.From, message.Text);
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